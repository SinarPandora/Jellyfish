using Jellyfish.Core.Command;
using Jellyfish.Core.Data;
using Jellyfish.Util;
using Kook;
using Kook.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Module.TeamPlay.User;

/// <summary>
///     Teammate join to team play room command
///     Grant permission for teammate if the text channel is private (voice room has password)
/// </summary>
public class TeamPlayTeammateJoinCommand(
    ILogger<TeamPlayTeammateJoinCommand> log,
    BaseSocketClient kook,
    DbContextProvider dbProvider)
    : UserConnectEventCommand
{
    public override string Name() => "队友加入组队语音频道指令";

    public override async Task<CommandResult> Execute(Cacheable<SocketGuildUser, ulong> user,
        SocketVoiceChannel channel, DateTimeOffset joinAt)
    {
        await using var dbCtx = dbProvider.Provide();
        var room = dbCtx.TpRoomInstances.Include(e => e.TmpTextChannel)
            .FirstOrDefault(room => room.VoiceChannelId == channel.Id && room.GuildId == channel.Guild.Id);

        if (room?.TmpTextChannel is null) return CommandResult.Continue;

        room.UpdateTime = DateTime.Now; // Any user enter will refresh the expiration time
        var tmpInstance = room.TmpTextChannel;
        var restGuild = await kook.Rest.GetGuildAsync(channel.Guild.Id);
        var restTextChannel = await restGuild.GetTextChannelAsync(tmpInstance.ChannelId);
        if (
            restTextChannel is null
            || restTextChannel.GetPermissionOverwrite(user.Value!) is not null
        ) return CommandResult.Done;

        if (channel.HasPassword)
        {
            // If voice room has a password, grant text room access permission to user
            await restTextChannel.OverrideUserPermissionAsync(user.Value!, p => p.Modify(
                viewChannel: PermValue.Allow,
                mentionEveryone: PermValue.Allow
            ));
            log.LogInformation("加入语音房间 {TpRoomName} 的用户：{Name}:{Id}，已获得文字房间 {TextRoomName} 的访问权限",
                room.RoomName, user.Value!.DisplayName(), user.Id, restTextChannel.Name
            );
            await Task.Delay(TimeSpan.FromSeconds(3)); // Delay 3s for Kook app cache refresh
            await restTextChannel.SendSuccessCardAsync(
                $"""
                 欢迎 {MentionUtils.KMarkdownMentionUser(user.Id)} 加入组队房间！
                 ---
                 若新成员无法看到该文字房间，文字房间内的成员可尝试艾特他/她
                 """,
                false);
        }
        else
        {
            await restTextChannel.OverrideUserPermissionAsync(user.Value!, p => p.Modify(
                sendMessages: PermValue.Allow
            ));
            log.LogInformation("加入语音房间 {TpRoomName} 的用户：{Name}:{Id}，已在文字房间 {TextRoomName} 中被标记",
                room.RoomName, user.Value!.DisplayName(), user.Id, restTextChannel.Name
            );
            await restTextChannel.SendSuccessCardAsync($"欢迎 {user.Value!.DisplayName()} 加入组队房间！",
                false);
        }

        return CommandResult.Done;
    }
}
