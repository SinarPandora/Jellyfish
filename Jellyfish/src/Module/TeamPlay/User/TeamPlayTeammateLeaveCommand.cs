using Jellyfish.Core.Command;
using Jellyfish.Core.Data;
using Jellyfish.Util;
using Kook;
using Kook.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Module.TeamPlay.User;

/// <summary>
///     Teammate leave to team play room command
///     Remove the text channel permission if the bound voice channel has password
/// </summary>
public class TeamPlayTeammateLeaveCommand(
    ILogger<TeamPlayTeammateLeaveCommand> log,
    BaseSocketClient kook,
    DbContextProvider dbProvider)
    : UserDisconnectEventCommand
{
    public override string Name() => "队友退出组队语音频道指令";

    public override async Task<CommandResult> Execute(Cacheable<SocketGuildUser, ulong> user,
        SocketVoiceChannel channel,
        DateTimeOffset leaveAt)
    {
        await using var dbCtx = dbProvider.Provide();
        var room = dbCtx.TpRoomInstances.Include(e => e.TmpTextChannel)
            .FirstOrDefault(room => room.VoiceChannelId == channel.Id && room.GuildId == channel.Guild.Id);

        if (room?.TmpTextChannel is null || !channel.HasPassword) return CommandResult.Continue;

        var restGuild = await kook.Rest.GetGuildAsync(channel.Guild.Id);
        var restTextChannel = await restGuild.GetTextChannelAsync(room.TmpTextChannel.ChannelId);

        if (restTextChannel is null) return CommandResult.Continue;

        await restTextChannel.RemoveUserPermissionOverrideAsync(user.Value!);
        log.LogInformation("已移除属于 {UserName} 私有组队房间 {RoomName}#{RoomId} 的文字频道访问权限",
            user.Value!.DisplayName(), room.RoomName, room.Id);

        dbCtx.SaveChanges();

        return CommandResult.Continue; // This is a middleware command, so make it continue event done
    }
}
