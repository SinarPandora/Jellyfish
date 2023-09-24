using Jellyfish.Core.Cache;
using Jellyfish.Core.Command;
using Jellyfish.Core.Data;
using Jellyfish.Module.TeamPlay.Core;
using Jellyfish.Util;
using Kook;
using Kook.WebSocket;

namespace Jellyfish.Module.TeamPlay.User;

/// <summary>
///     Team play command for normal user, use to create room instance
/// </summary>
public class TeamPlayUserCommand : MessageCommand
{
    private readonly TeamPlayRoomService _service;

    public TeamPlayUserCommand(TeamPlayRoomService service)
    {
        _service = service;
        HelpMessage = HelpMessageTemplate.ForMessageCommand(this,
            """
            组队指令

            欢迎使用组队指令！
            本房间的默认人数为：%s人

            指令举例：
            /组队
            /组队 开放随便打打
            /组队 无限制 随机武器私房
            /组队 6 比赛训练房间
            /组队 密码 2335
            """,
            """
            - 帮助：显示此消息
            - [房间名]：创建房间
            - [人数] [房间名]：创建指定人数的房间
            - 密码 [1~12 位数字]：为自己创建的房间设置密码
            """);
    }

    public override string Name() => "组队房间指令";

    public override IEnumerable<string> Keywords() => new[] { "/组队" };

    public override async Task Execute(string args, SocketMessage msg, SocketGuildUser user, SocketTextChannel channel)
    {
        if (args.StartsWith("帮助"))
            await channel.SendTextAsync(HelpMessage);
        else if (args.StartsWith("密码"))
            await SetPassword(args[2..].Trim(), user, channel);
        else
            await CreateRoom(msg, user, channel, args);
    }

    /// <summary>
    ///     Create room instance
    /// </summary>
    /// <param name="rawMessage">Raw message</param>
    /// <param name="user">Current user</param>
    /// <param name="channel">Current text channel</param>
    /// <param name="rawArgs">Raw create room args in string</param>
    private static async Task CreateRoom(SocketMessage rawMessage, SocketGuildUser user, SocketTextChannel channel,
        string rawArgs)
    {
        await using var dbCtx = new DatabaseContext();

        var argsBuilder = CreateRoomCommandParser.Parse(rawArgs);
        var tpConfig = (from config in AppCaches.TeamPlayConfigs.Values
            where config.TextChannelId == channel.Id
            select config).FirstOrDefault();
        if (tpConfig == null) return;

        await TeamPlayRoomService.CreateRoomWithCommand(argsBuilder(tpConfig), rawMessage, user, channel,
            async (ins, room) =>
            {
                var invite = await room.CreateInviteAsync(InviteMaxAge.NeverExpires);
                var card = new CardBuilder();
                card.AddModule<HeaderModuleBuilder>(m => { m.Text = $"房间已创建：{ins.RoomName}"; });
                card.AddModule<InviteModuleBuilder>(m => { m.Code = invite.Code; });
                await channel.SendCardAsync(card.Build());
            });
    }

    /// <summary>
    ///     Set room password
    /// </summary>
    /// <param name="password">Room password</param>
    /// <param name="user">Current user</param>
    /// <param name="channel">Current channel</param>
    private async Task SetPassword(string password, SocketGuildUser user, IMessageChannel channel)
    {
        await _service.SetRoomPassword(password, user, channel, async () =>
        {
            if (password.IsEmpty()) await channel.SendSuccessCardAsync("已移除房间密码");
            else await channel.SendSuccessCardAsync("已设置房间密码");
        });
    }
}
