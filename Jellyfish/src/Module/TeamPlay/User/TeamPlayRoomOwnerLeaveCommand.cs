using Jellyfish.Core.Command;
using Jellyfish.Core.Data;
using Jellyfish.Util;
using Kook;
using Kook.WebSocket;

namespace Jellyfish.Module.TeamPlay.User;

/// <summary>
///     Room owner leave channel command.
///     When owner leaved, remove its owner permission
/// </summary>
public class TeamPlayRoomOwnerLeaveCommand : UserDisconnectEventCommand
{
    public override string Name() => "房主离开语音房间指令";

    public override async Task<CommandResult> Execute(Cacheable<SocketGuildUser, ulong> user,
        SocketVoiceChannel channel, DateTimeOffset leaveAt)
    {
        await using var dbCtx = new DatabaseContext();
        var room = (from instance in dbCtx.TpRoomInstances
            where instance.GuildId == channel.Guild.Id && instance.VoiceChannelId == channel.Id
            select instance).FirstOrDefault();

        if (room == null || room.OwnerId != user.Value.Id) return CommandResult.Continue;

        // Remove owner permission
        await channel.RemovePermissionOverwriteAsync(user.Value);
        room.OwnerId = 0;
        dbCtx.SaveChanges();

        var dmc = await user.Value.CreateDMChannelAsync();
        if (channel.Users.All(u => u.Id == user.Value.Id || (u.IsBot ?? false)))
        {
            await dmc.SendInfoCardAsync($"您已离开当前房间 {room.RoomName}，感谢您的使用");
        }
        else
        {
            await dmc.SendInfoCardAsync($"您已离开当前房间 {room.RoomName}，您的房主权限将稍后传递给房间内的下一个人，感谢您的使用");
        }

        return CommandResult.Done;
    }
}
