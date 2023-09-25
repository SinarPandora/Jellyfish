using Jellyfish.Core.Command;
using Jellyfish.Module.TeamPlay.Core;
using Jellyfish.Util;
using Kook.WebSocket;

namespace Jellyfish.Module.TeamPlay.User;

/// <summary>
///     DMC command, for room owner change room information
/// </summary>
public class TeamPlayRoomUpdateDmcCommand : DmcCommand
{
    private readonly TeamPlayRoomService _service;

    public TeamPlayRoomUpdateDmcCommand(TeamPlayRoomService service)
    {
        _service = service;
    }

    public override string Name() => "房主私聊修改房间信息指令";

    protected override IEnumerable<string> Keywords() => new[] { "/改名", "/密码", "/人数", "/解散" };

    protected override Task Execute(string args, string keyword, SocketMessage msg, SocketUser user,
        SocketDMChannel channel)
    {
        return keyword switch
        {
            "/改名" => _service.UpdateRoomNameAsync(args, user, channel,
                () => channel.SendSuccessCardAsync($"房间名已修改为 {args}")),
            "/密码" => _service.SetRoomPasswordAsync(args, user, channel,
                async () =>
                {
                    if (args.IsEmpty()) await channel.SendSuccessCardAsync("已移除房间密码");
                    else await channel.SendSuccessCardAsync("已设置房间密码");
                }),
            "/人数" => _service.UpdateRoomMemberLimitAsync(args, user, channel,
                () => channel.SendSuccessCardAsync($"房间人数已设置为 {args}")),
            "/解散" => _service.DissolveRoomInstanceAsync(user, channel,
                () => channel.SendSuccessCardAsync("您已解散房间")),
            _ => Task.CompletedTask
        };
    }
}
