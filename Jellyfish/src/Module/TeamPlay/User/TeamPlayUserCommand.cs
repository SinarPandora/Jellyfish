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
public class TeamPlayUserCommand : GuildMessageCommand
{
    private const string HelpTemplate =
        """

        **可选参数：**
        [人数] [房间名] [密码]

        **参数解释：**
        1. 房间名：建议不超过 12 字
        2. 人数：1~99 整数，或 “无限制”（该频道房间默认%s人）
        3. 密码：1~12 位纯数字

        ```
        ⚠️ 当不指定房间名，但需要设置人数或密码时，人数或密码需要加上对应名称作为前缀。
        ```

        ---
        以下是有效的指令：

        **直接开房：**
        ```
        /组队
        ```

        **指定房间名称：**
        ```
        1. /组队 开放随便打打
        2. /组队 2 塔楼活动比赛随便来
        3. /组队 无限制 菇的随机武器私房 2335
        4. /组队 私聊唠嗑 1234
        ```

        **不指定房间名称：**
        ```
        1. /组队 人数 6
        2. /组队 密码 2335
        3. /组队 人数 6 密码 2335
        ```
        """;

    private readonly TeamPlayRoomService _service;

    public TeamPlayUserCommand(TeamPlayRoomService service)
    {
        _service = service;
        HelpMessage = "请通过 Help 方法生成帮助信息";
    }

    public override string Name() => "组队房间指令";

    public override IEnumerable<string> Keywords() => new[] { "/组队" };

    protected override async Task Execute(string args, string keyword, SocketMessage msg, SocketGuildUser user,
        SocketTextChannel channel)
    {
        if (args.StartsWith("帮助"))
            await Help(channel);
        else
            await CreateRoom(user, channel, args);
    }

    /// <summary>
    ///     Show help message
    /// </summary>
    /// <param name="channel">Current channel to find config</param>
    private async Task Help(SocketTextChannel channel)
    {
        var tpConfig = (from config in AppCaches.TeamPlayConfigs.Values
            where config.GuildId == channel.Guild.Id && config.TextChannelId == channel.Id
            select config).FirstOrDefault();
        if (tpConfig == null)
        {
            var configs =
                (from config in AppCaches.TeamPlayConfigs.Values
                    where config.GuildId == channel.Guild.Id && config.TextChannelId != null
                    select MentionUtils.KMarkdownMentionChannel((ulong)config.TextChannelId!)
                ).ToArray();

            if (configs.IsEmpty())
            {
                await channel.SendInfoCardAsync("当前服务器没有开启组队功能");
            }
            else
            {
                await channel.SendInfoCardAsync(
                    $"""
                     当前频道没有配置组队功能，请前往以下频道使用该功能：
                     {string.Join('\n', configs)}
                     """);
            }

            return;
        }

        var help = HelpTemplate.Format(tpConfig.DefaultMemberLimit == 0
            ? "无限制"
            : tpConfig.DefaultMemberLimit.ToString());
        await channel.SendTextAsync(HelpMessageTemplate.ForMessageCommand(this, "欢迎使用组队指令！", help));
    }

    /// <summary>
    ///     Create room instance
    /// </summary>
    /// <param name="user">Current user</param>
    /// <param name="channel">Current text channel</param>
    /// <param name="rawArgs">Raw create room args in string</param>
    private async Task CreateRoom(SocketGuildUser user, SocketTextChannel channel, string rawArgs)
    {
        await using var dbCtx = new DatabaseContext();

        var argsBuilder = CreateRoomCommandParser.Parse(rawArgs);
        var tpConfig = (from config in AppCaches.TeamPlayConfigs.Values
            where config.GuildId == channel.Guild.Id && config.TextChannelId == channel.Id
            select config).FirstOrDefault();
        if (tpConfig == null) return;

        await _service.CreateAndMoveToRoomAsync(argsBuilder(tpConfig), user, channel,
            async (_, room) =>
            {
                await channel.SendCardAsync(await TeamPlayRoomService.CreateInviteCardAsync(room));
                await channel.SendTextAsync($"{MentionUtils.KMarkdownMentionUser(user.Id)} 👍🏻请点击上方「加入」按钮进入房间");
            });
    }
}
