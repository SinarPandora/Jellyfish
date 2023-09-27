using FluentScheduler;
using Jellyfish.Core.Data;
using Jellyfish.Module.TeamPlay.Core;
using Jellyfish.Module.TeamPlay.Data;
using Jellyfish.Util;
using Kook.WebSocket;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace Jellyfish.Module.TeamPlay.Job;

/// <summary>
///     Room scan job, scan all registered room's remove all empty
/// </summary>
public class TeamPlayRoomScanJob : IAsyncJob
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly KookSocketClient _client;

    public TeamPlayRoomScanJob(KookSocketClient client)
    {
        _client = client;
    }

    /// <summary>
    ///     Entrypoint for AsyncJob
    /// </summary>
    public async Task ExecuteAsync()
    {
        var now = DateTime.Now;
        await using var dbCtx = new DatabaseContext();
        var configs = dbCtx.TpConfigs.Include(e => e.RoomInstances)
            .GroupBy(e => e.GuildId)
            .ToDictionary(
                e => e.Key,
                e => e.SelectMany(c => c.RoomInstances)
            );

        foreach (var (guildId, rooms) in configs)
        {
            var guild = _client.GetGuild(guildId);
            foreach (var room in rooms)
            {
                // 2 minutes as timeout in order not to clean up room just created
                if (room.CreateTime.AddMinutes(2) >= now) continue;
                await CheckAndDeleteRoom(guild, room, dbCtx);
                dbCtx.SaveChanges(); // Save immediately for each room
            }
        }
    }

    /// <summary>
    ///     Check and delete room
    /// </summary>
    /// <param name="guild">Current guild</param>
    /// <param name="room">Room instance</param>
    /// <param name="dbCtx">Database context</param>
    private static async Task CheckAndDeleteRoom(SocketGuild guild, TpRoomInstance room, DatabaseContext dbCtx)
    {
        var voiceChannel = guild.GetVoiceChannel(room.VoiceChannelId);
        try
        {
            // 1. Check if room not exist
            if (voiceChannel == null)
            {
                dbCtx.TpRoomInstances.Remove(room);
                return;
            }

            // 2. Check if room empty
            var users = await voiceChannel.GetConnectedUsersAsync();
            if (users.All(u => u.IsBot ?? false))
            {
                Log.Info($"检测到房间 {room.RoomName} 只剩 bot 自己，开始清理房间");
                await guild.DeleteVoiceChannelAsync(room.VoiceChannelId);
                dbCtx.TpRoomInstances.Remove(room);
                Log.Info($"已删除房间：{room.RoomName}");
            }

            // 3. Check if owner leave
            else if (users.Count > 0 && users.All(u => u.Id != room.OwnerId))
            {
                // If room owner not in the room, switch owner
                var newOwner =
                    (from user in users
                        where !(user.IsBot ?? false)
                        select user).FirstOrDefault();
                if (newOwner == null) return;

                Log.Info($"检测到房主离开房间 {room.RoomName}，将随机产生新房主");
                await TeamPlayRoomService.GiveOwnerPermissionAsync(voiceChannel, newOwner);
                room.OwnerId = newOwner.Id;
                await TeamPlayRoomService.SendRoomUpdateWizardToDmcAsync(
                    await newOwner.CreateDMChannelAsync(),
                    room.RoomName
                );
                Log.Info($"新房主已产生，房间：{room.RoomName}，房主：{newOwner.DisplayName()}");
            }

            // 4. Check room name with 🔐locked icon if it has password(and also sync the name)
            var newRoomName = voiceChannel.Name;
            if (voiceChannel.HasPassword)
            {
                if (!newRoomName.StartsWith("🔐"))
                {
                    newRoomName = $"🔐{room.RoomName}";
                }
            }
            else if (newRoomName.StartsWith("🔐"))
            {
                newRoomName = newRoomName.ReplaceFirst("🔐", string.Empty);
            }

            room.RoomName = newRoomName;
        }
        catch (Exception e)
        {
            Log.Error(e, $"尝试清理房间失败，房间名：{room.RoomName}");
        }
    }
}
