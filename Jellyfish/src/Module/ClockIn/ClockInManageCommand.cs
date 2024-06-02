using Jellyfish.Core.Command;
using Jellyfish.Module.ClockIn.Core;
using Jellyfish.Util;
using Kook.WebSocket;

namespace Jellyfish.Module.ClockIn;

/// <summary>
///     Command for manage clock-in
/// </summary>
public class ClockInManageCommand : GuildMessageCommand
{
    private readonly ClockInManageService _service;

    public ClockInManageCommand(ClockInManageService service) : base(true)
    {
        _service = service;
        HelpMessage = HelpMessageHelper.ForMessageCommand(this,
            """
            打卡配置指令

            现在您可以为 Kook 服务器增加打卡功能了！
            就像常见软件的打卡功能，用户每天可以发送 `/打卡` 指令进行一次打卡。

            为方便管理员利用打卡功能举办活动，打卡模块支持以下功能：

            1. 置底打卡消息
            您可以使用 `！打卡 发送` 指令，向当前频道发送一个带有打卡按钮的卡片消息，该卡片将持续置于频道底部，确保用户进入频道后能第一时间看到。
            该卡片会持续更新显示今日打卡人数，累积打卡人数，以及每日打卡排行榜。
            该卡片的「标题」和「详情信息」均可配置，消息被管理员删除后将不再发送。

            2. 打卡阶段
            打卡阶段指的是连续/非连续的持续打卡次数，使用 `！打卡阶段` 指令配置。
            您可以利用该功能设置持续 N 天的打卡活动，例如：用户在本月内打卡 25 天即可满足条件参与抽奖。
            当用户满足了指定次数后，Bot 会通知该用户「您已合格」（该消息可配置），您也可以随时查询满足条件的用户。
            打卡阶段支持设置「时间段」和「允许中断天数」，也可以配置为当用户满足条件后自动记录到指定频道。
            同时打卡阶段可以设置多个，方便同时进行多个活动。
            """,
            """
            基础配置：
            1. 启用：为该服务器开启打卡功能（未开启前，用户发送 `/打卡` 指令不会有任何效果）
            2. 禁用：为该服务器关闭打卡功能
            3. 排行 [大于0整数，默认 10]：列出当前服务器累积打卡最多的前 N 名用户（并列用户将同时列出）
            ---
            打卡消息配置：
            1. 标题：打卡卡片消息标题（默认为：每日打卡）
            2. 详情：打卡卡片消息详情内容
            3. 发送：发送打卡消息到当前频道（每个频道只能存在一条）
            4. 按钮名称：打卡按钮名称（默认为：打卡！）
            """
        );
    }

    public override string Name() => "打卡管理指令";

    public override IEnumerable<string> Keywords() => ["!打卡", "！打卡"];

    protected override async Task Execute(string args, string keyword, SocketMessage msg, SocketGuildUser user,
        SocketTextChannel channel)
    {
        if (args.StartsWith(HelpMessageHelper.HelpCommand))
        {
            await channel.SendCardSafeAsync(HelpMessage);
            return;
        }

        var isSuccess = true;
        if (args.StartsWith("启用"))
            isSuccess = await _service.Enable(channel);
        else if (args.StartsWith("禁用"))
            isSuccess = await _service.Disable(channel);
        else if (args.StartsWith("发送"))
            isSuccess = await _service.SendCard(channel);
        else if (args.StartsWith("标题"))
            isSuccess = await _service.SetCardTitle(channel, args[2..].TrimStart());
        else if (args.StartsWith("详情"))
            isSuccess = await _service.SetCardDescription(channel, args[2..].TrimStart());
        else if (args.StartsWith("按钮名称"))
            isSuccess = await _service.SetCardButtonName(channel, args[4..].TrimStart());
        else if (args.StartsWith("排行"))
            isSuccess = await _service.ListTopUsers(channel, args[2..].TrimStart());
        else
            await channel.SendCardAsync(HelpMessage);

        if (!isSuccess)
        {
            _ = channel.DeleteMessageWithTimeoutAsync(msg.Id);
        }
    }
}
