using Jellyfish.Core.Cache;
using Jellyfish.Core.Command;
using Jellyfish.Module.GuildSetting.Enum;
using Jellyfish.Util;
using Kook.WebSocket;

namespace Jellyfish.Custom.Splatoon.Model.SuiteSearch;

/// <summary>
///     Kook command for suite-build search
/// </summary>
public class SuiteSearchCommand : GuildMessageCommand
{
    private readonly SuiteSearchService _service;

    public SuiteSearchCommand(SuiteSearchService service) : base(false)
    {
        _service = service;
        HelpMessage = HelpMessageTemplate.ForMessageCommand(this,
            """
            Splatoon3 配装查询指令

            您可以使用武器原名称或常用别名查询常用配装
            查询贴牌武器请添加「贴牌」前缀，如 /配装 贴牌红双
            数据来源：https://sendou.ink
            """,
            "/配装 [武器名称或别名]：查询指定武器的常用配装");
    }

    public override string Name() => "斯普拉遁3配装查询指令";

    public override IEnumerable<string> Keywords() => ["/配装", "/装备", "/套装"];

    protected override async Task Execute(string args, string keyword, SocketMessage msg, SocketGuildUser user,
        SocketTextChannel channel)
    {
        if (AppCaches.GuildSettings[channel.Guild.Id].EnabledFeatures.Contains(GuildCustomFeature.SplatoonGame))
        {
            if (args.StartsWith("帮助") || string.IsNullOrWhiteSpace(args))
            {
                await channel.SendCardSafeAsync(HelpMessage);
            }
            else
            {
                await _service.Search(args, channel, user);
            }
        }
    }
}
