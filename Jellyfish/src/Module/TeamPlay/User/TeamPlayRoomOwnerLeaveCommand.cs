using Jellyfish.Core.Command;
using Jellyfish.Core.Data;
using Jellyfish.Util;
using Kook;
using Kook.WebSocket;

namespace Jellyfish.Module.TeamPlay.User;

/// <summary>
///     Room owner leave channel command.
///     When the owner left, remove its owner permission
/// </summary>
public class TeamPlayRoomOwnerLeaveCommand(ILogger<TeamPlayRoomOwnerLeaveCommand> log, DbContextProvider dbProvider)
    : UserDisconnectEventCommand
{
    public override string Name() => "房主离开语音房间指令";

    public override async Task<CommandResult> Execute(Cacheable<SocketGuildUser, ulong> user,
        SocketVoiceChannel channel, DateTimeOffset leaveAt)
    {
        await using var dbCtx = dbProvider.Provide();
        var room = (from instance in dbCtx.TpRoomInstances
            where instance.GuildId == channel.Guild.Id && instance.VoiceChannelId == channel.Id
            select instance).FirstOrDefault();

        if (room == null || room.OwnerId != user.Value!.Id) return CommandResult.Continue;

        // Remove owner permission
        await channel.RemoveUserPermissionOverrideAsync(user.Value);
        room.OwnerId = 0;
        dbCtx.SaveChanges();

        log.LogInformation("房主 {UserName} 已离开房间 {RoomName}#{Id}", user.Value.DisplayName(), room.RoomName, room.Id);

        return CommandResult.Continue; // This is a middleware command, so make it continue event done
    }
}
