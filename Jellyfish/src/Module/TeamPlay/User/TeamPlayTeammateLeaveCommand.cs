using Jellyfish.Core.Command;
using Jellyfish.Core.Data;
using Jellyfish.Util;
using Kook;
using Kook.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Module.TeamPlay.User;

/// <summary>
///     Teammate leave to team play room command
///     Remove the text channel permission if bound voice channel has password
/// </summary>
public class TeamPlayTeammateLeaveCommand : UserDisconnectEventCommand
{
    private readonly DatabaseContext _dbCtx;
    private readonly ILogger<TeamPlayTeammateLeaveCommand> _log;
    private readonly KookSocketClient _kook;

    public TeamPlayTeammateLeaveCommand(DatabaseContext dbCtx, ILogger<TeamPlayTeammateLeaveCommand> log,
        KookSocketClient kook)
    {
        _dbCtx = dbCtx;
        _log = log;
        _kook = kook;
    }

    public override string Name() => "队友退出组队语音频道指令";

    public override async Task<CommandResult> Execute(Cacheable<SocketGuildUser, ulong> user,
        SocketVoiceChannel channel,
        DateTimeOffset leaveAt)
    {
        var room = _dbCtx.TpRoomInstances.Include(e => e.TmpTextChannel)
            .FirstOrDefault(room => room.VoiceChannelId == channel.Id && room.GuildId == channel.Guild.Id);

        if (room?.TmpTextChannel == null || !channel.HasPassword) return CommandResult.Continue;

        var restGuild = await _kook.Rest.GetGuildAsync(channel.Guild.Id);
        var restTextChannel = await restGuild.GetTextChannelAsync(room.TmpTextChannel.ChannelId);

        if (restTextChannel == null) return CommandResult.Continue;

        await restTextChannel.RemoveUserPermissionOverrideAsync(user.Value);
        _log.LogInformation("已移除属于 {UserName} 私有组队房间 {RoomName}#{RoomId} 的文字频道访问权限",
            user.Value.DisplayName(), room.RoomName, room.Id);

        return CommandResult.Continue; // This is a middleware command, so make it continue event done
    }
}
