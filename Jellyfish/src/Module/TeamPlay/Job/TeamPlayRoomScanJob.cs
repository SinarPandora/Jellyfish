using FluentScheduler;
using Jellyfish.Core.Cache;
using Jellyfish.Core.Data;
using Jellyfish.Module.TeamPlay.Core;
using Jellyfish.Module.TeamPlay.Data;
using Jellyfish.Util;
using Kook;
using Kook.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Module.TeamPlay.Job;

/// <summary>
///     Room scan job, scan all registered room's remove all empty
/// </summary>
public class TeamPlayRoomScanJob(BaseSocketClient kook, ILogger<TeamPlayRoomScanJob> log, DbContextProvider dbProvider)
    : IAsyncJob
{
    private const int TextChannelExpireDuration = 20;

    /// <summary>
    ///     Entrypoint for AsyncJob
    /// </summary>
    public async Task ExecuteAsync()
    {
        log.LogInformation("组队房间扫描任务开始");
        var now = DateTime.Now;

        await using var dbCtx = dbProvider.Provide();
        var configs = dbCtx.TpConfigs
            .Include(e => e.RoomInstances)
            .ThenInclude(e => e.TmpTextChannel)
            .GroupBy(e => e.GuildId)
            .ToDictionary(
                e => e.Key,
                e => e.SelectMany(c => c.RoomInstances)
            );

        foreach (var (guildId, rooms) in configs)
        {
            var guild = kook.GetGuild(guildId);
            foreach (var room in rooms)
            {
                await CheckAndDeleteRoom(guild, room, now, dbCtx);
                dbCtx.SaveChanges(); // Save immediately for each room
            }
        }

        // Remove expired creation locks
        Locks.RoomCreationLock.RemoveWhere(pair => pair.Value.AddSeconds(10) < now);

        log.LogInformation("组队房间扫描任务结束");
    }

    /// <summary>
    ///     Check and delete room
    /// </summary>
    /// <param name="guild">Current guild</param>
    /// <param name="room">Room instance</param>
    /// <param name="now">Now datetime</param>
    /// <param name="dbCtx">Database context</param>
    private async Task CheckAndDeleteRoom(SocketGuild guild, TpRoomInstance room, DateTime now, DatabaseContext dbCtx)
    {
        var voiceChannel = guild.GetVoiceChannel(room.VoiceChannelId);
        var textChannel = room.TmpTextChannel != null ? guild.GetTextChannel(room.TmpTextChannel.ChannelId) : null;
        try
        {
            // 1. Sync text channel status to team play room instance
            if (textChannel == null)
            {
                room.TmpTextChannelId = null;
            }

            // 2. If voice channel does not exist, clean the room instance
            if (voiceChannel == null)
            {
                if (textChannel != null)
                {
                    await textChannel.DeleteAsync();
                }

                dbCtx.TpRoomInstances.Remove(room);
                return;
            }

            // 3. Check if any user in room
            var users = await voiceChannel.GetConnectedUsersAsync()!;
            if (users.Any(u => !(u.IsBot ?? false)))
            {
                // 4. Refresh the update time to delay the room cleanup
                room.UpdateTime = DateTime.Now;

                // 5. Check if owner leave
                if (users.All(u => u.Id != room.OwnerId))
                {
                    await ElectNewRoomOwner(room, users, voiceChannel, textChannel);
                }
            }
            else
            {
                // 6. If no user in the room
                var needCleanup = room.UpdateTime.AddMinutes(TextChannelExpireDuration) < now
                                  && (textChannel == null || await IsLatestMessageBefore(
                                      textChannel, now, TextChannelExpireDuration)
                                  );

                if (needCleanup)
                {
                    await CleanUpTeamPlayRoom(guild, room, textChannel, voiceChannel, dbCtx);
                    return; // Break the method
                }
            }

            // 7. Check room name with 🔐locked icon if it has password (and also sync the name for text channel)
            await RefreshChannelNames(room, voiceChannel, textChannel);

            // 8. Sync member permission for private text channel (which bound voice channel has password)
            if (users.IsNotEmpty() && textChannel != null)
            {
                await SyncPrivateTextChannelMemberPermission(
                    voiceChannel,
                    textChannel,
                    users.Where(u => !u.IsBot ?? false).ToArray());
            }
        }
        catch (Exception e)
        {
            log.LogError(e, "组队房间扫描任务失败，房间名：{RoomName}", room.RoomName);
        }
    }

    /// <summary>
    ///     If the bound voice room comes with a password， sync member permission for text channel
    /// </summary>
    /// <param name="voiceChannel">Voice channel</param>
    /// <param name="textChannel">Target text channel</param>
    /// <param name="users">Users in voice room</param>
    private static async Task SyncPrivateTextChannelMemberPermission(SocketVoiceChannel voiceChannel,
        SocketTextChannel textChannel, IEnumerable<SocketGuildUser> users)
    {
        var cachedGuild = voiceChannel.Guild;
        var everyOneRole = cachedGuild.EveryoneRole;
        var everyoneOverride = textChannel.GetPermissionOverwrite(everyOneRole);
        switch (voiceChannel.HasPassword)
        {
            case true when everyoneOverride is not { ViewChannel: PermValue.Deny }:
            {
                // Sync voice members and synergy bot accounts permission to text channel, then hide the text channel
                await Task.WhenAll(
                    Task.WhenAll(users.Select(user =>
                        textChannel.OverrideUserPermissionAsync(user, r => r.Modify(
                            viewChannel: PermValue.Allow,
                            mentionEveryone: PermValue.Allow
                        )))
                    ),
                    Task.WhenAll(AppCaches.GuildSettings[cachedGuild.Id].SynergyBotAccounts.Select(botId =>
                    {
                        var botUser = cachedGuild.GetUser(botId);
                        if (botUser != null)
                        {
                            return textChannel.OverrideUserPermissionAsync(botUser, p =>
                                p.Modify(
                                    viewChannel: PermValue.Allow,
                                    mentionEveryone: PermValue.Allow
                                ));
                        }

                        return Task.CompletedTask;
                    }))
                );


                await textChannel.OverrideRolePermissionAsync(everyOneRole, r => r.Modify(viewChannel: PermValue.Deny));
                break;
            }
            case false when everyoneOverride is { ViewChannel: PermValue.Deny }:
            {
                // Remove all member override on bound text channel, and show the text channel for all user again
                await textChannel.SyncPermissionsAsync();
                break;
            }
        }
    }

    /// <summary>
    ///     Clean up team play room channels in guild
    /// </summary>
    /// <param name="guild">Current guild</param>
    /// <param name="room">Team play room instance</param>
    /// <param name="textChannel">Bound text channel</param>
    /// <param name="voiceChannel">Bound voice channel</param>
    /// <param name="dbCtx">Database context</param>
    private async Task CleanUpTeamPlayRoom(IGuild guild, TpRoomInstance room, IChannel? textChannel,
        IChannel voiceChannel, DatabaseContext dbCtx)
    {
        log.LogInformation("检测到房间 {RoomName} 已无人使用，开始清理房间", room.RoomName);
        if (textChannel != null)
        {
            await guild.DeleteSingleChannelAsync(textChannel.Id, ChannelType.Text);
            log.LogInformation("已删除文字房间：{RoomName}", textChannel.Name);
        }

        await guild.DeleteSingleChannelAsync(room.VoiceChannelId, ChannelType.Voice);
        log.LogInformation("已删除语音房间：{RoomName}", voiceChannel.Name);
        dbCtx.TpRoomInstances.Remove(room);
        log.LogInformation("已删除组队房间：{RoomName}", room.RoomName);
    }

    /// <summary>
    ///     Is the latest message in text channel before given
    /// </summary>
    /// <param name="textChannel"></param>
    /// <param name="now"></param>
    /// <param name="durationInMinute"></param>
    /// <returns></returns>
    private static async Task<bool> IsLatestMessageBefore(IMessageChannel textChannel, DateTime now,
        int durationInMinute)
    {
        var messages = await textChannel.GetMessagesAsync(1).FirstAsync();
        var lastMessage = messages.IsNotEmpty() ? messages.First() : null;
        return lastMessage == null || lastMessage.Timestamp.AddMinutes(durationInMinute) < now;
    }

    /// <summary>
    ///     Refresh voice room name
    ///     If room has password but not start with password icon, add it;
    ///     Or else remove it.
    /// </summary>
    /// <param name="room">Room instance</param>
    /// <param name="voiceChannel">Current voice channel</param>
    /// <param name="textChannel">Bound text channel</param>
    private async Task RefreshChannelNames(TpRoomInstance room, SocketVoiceChannel voiceChannel,
        SocketTextChannel? textChannel)
    {
        var cleanName = voiceChannel.Name;
        if (cleanName.StartsWith("🔐"))
        {
            cleanName = cleanName.ReplaceFirst("🔐", string.Empty);
        }
        else if (cleanName.StartsWith("🔊"))
        {
            cleanName = cleanName.ReplaceFirst("🔊", string.Empty);
        }

        var newName = (voiceChannel.HasPassword ? "🔐" : "🔊") + cleanName;

        if (newName != voiceChannel.Name)
        {
            log.LogInformation("监测到房间 {RoomName}#{Id} 名称发生变化，尝试更新房间名", room.RoomName, room.Id);
            await voiceChannel.ModifyAsync(v => v.Name = newName);
            if (textChannel != null)
            {
                var newTextChannelName = (voiceChannel.HasPassword ? "🔐" : "💬") + cleanName;
                if (newTextChannelName != textChannel.Name)
                {
                    await textChannel.ModifyAsync(c => c.Name = newTextChannelName);
                }
            }

            log.LogInformation("房间 {RoomName}#{Id} 名称已更新为 {NewName}", room.RoomName, room.Id, newName);
        }

        room.RoomName = newName;
    }

    /// <summary>
    ///     Elect new room owner if the latest owner leave
    /// </summary>
    /// <param name="instance">The room instance</param>
    /// <param name="users">All users in the room</param>
    /// <param name="voiceChannel">The voice channel</param>
    /// <param name="textChannel">The text channel</param>
    private async Task ElectNewRoomOwner(TpRoomInstance instance, IEnumerable<IGuildUser> users,
        IVoiceChannel voiceChannel, IMessageChannel? textChannel)
    {
        // If room owner not in the room, switch owner
        var newOwner =
            (from user in users
                where !(user.IsBot ?? false)
                select user).FirstOrDefault();
        if (newOwner != null)
        {
            log.LogInformation("检测到房主离开房间 {RoomName}，将随机产生新房主", instance.RoomName);
            await TeamPlayRoomService.GiveOwnerPermissionAsync(voiceChannel, newOwner);
            instance.OwnerId = newOwner.Id;

            if (textChannel != null)
            {
                await textChannel.SendInfoCardAsync($"由于上一任房主已经离开语音房间，{newOwner.DisplayName()} 已成为新语音房间房主", false);
            }
            else
            {
                var dmc = await newOwner.CreateDMChannelAsync();
                await dmc.SendInfoCardAsync(
                    $"由于上一任房主已经离开语音房间，{newOwner.DisplayName()} 已成为组队房间 {instance.RoomName} 新语音房间房主", false);
            }

            log.LogInformation("新房主已产生，房间：{RoomName}，房主：{DisplayName}", instance.RoomName, newOwner.DisplayName());
        }
    }
}
