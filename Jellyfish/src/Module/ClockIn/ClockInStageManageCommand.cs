using Jellyfish.Core.Command;
using Jellyfish.Module.ClockIn.Core;
using Jellyfish.Util;
using Kook.WebSocket;

namespace Jellyfish.Module.ClockIn;

/// <summary>
///     Command for manage clock-in stage
/// </summary>
public class ClockInStageManageCommand : GuildMessageCommand
{
    private readonly ClockInStageManageService _service;

    public ClockInStageManageCommand(ClockInStageManageService service) : base(true)
    {
        _service = service;
        HelpMessage = HelpMessageHelper.ForMessageCommand(this,
            """
            管理打卡阶段
            ---
            打卡阶段指的是连续/非连续的持续打卡次数。
            ---
            您可以利用该功能设置持续 N 天的打卡活动，例如：用户在本月内打卡 25 天即可满足条件参与抽奖。
            当用户满足了指定次数后，Bot 会通知该用户「您已合格」（该消息可配置），您也可以随时查询满足条件的用户。
            打卡阶段支持设置「时间段」和「允许中断天数」，也可以配置为当用户满足条件后自动记录到指定频道。
            打卡阶段可以设置多个，方便同时进行多个活动。
            """,
            """
            基础功能：
            1. 列表 启用：列出全部启用的打卡阶段
            2. 列表 禁用：列出全部禁用的打卡阶段
            3. 创建 [名称] [开始日期（年-月-日）] [达标天数（大于 0 整数）]：创建新的打卡阶段
            4. 结果频道 [#频道引用]：当用户满足条件时，向该频道发送一条消息用作记录（删除频道后将不再发送）
            ---
            阶段信息配置：
            ！请注意：
            打卡扫描任务持续在后台运行，若修改「合格消息」和「给予身份」的过程中有人满足条件则可能不会应用更改。
            请务必在每次修改前禁用该阶段。
            ---
            1. [阶段ID] 开始日期 [日期（年-月-日）]：修改开始日期
            2. [阶段ID] 结束日期 [日期（年-月-日）]：修改结束日期（包含当天）
            3. [阶段ID] 达标天数 [大于 0 整数]：修改达标天数
            4. [阶段ID] 合格消息 [消息内容]：设置合格时向用户发送的消息
            5. [阶段ID] 给予身份 [@身份引用]：设置合格时颁发给用户的 Kook 角色
            6. [阶段ID] 允许中断天数 [大于或等于 0 的整数]：修改允许中断天数，设置为 0 时不允许中断打卡
            7. [阶段ID] 禁用：禁用该阶段
            8. [阶段ID] 启用：启用该阶段
            9. [阶段ID] 满足条件的用户：列出全部满足条件的用户（由于消息长度限制，当超过 50 名用户时只列出最多前 50 名，您可以设置合格身份，并在 Kook 的管理页面中使用身份进行过滤）
            """);
    }

    public override string Name() => "打卡阶段管理指令";

    public override IEnumerable<string> Keywords() => ["!打卡阶段", "！打卡阶段"];

    protected override async Task Execute(string args, string keyword, SocketMessage msg, SocketGuildUser user,
        SocketTextChannel channel)
    {
        if (args.StartsWith(HelpMessageHelper.HelpCommand))
        {
            await channel.SendCardSafeAsync(HelpMessage);
            return;
        }

        bool isSuccess;
        if (args.StartsWith("列表"))
            isSuccess = await _service.List(channel, args[2..].TrimStart());
        else if (args.StartsWith("创建"))
            isSuccess = await _service.Create(channel, args[2..].TrimStart());
        else if (args.StartsWith("结果频道"))
            isSuccess = await _service.SetResultChannel(channel, args[4..].TrimStart());
        else
            isSuccess = await DispatchSubCommand(args, channel);

        if (!isSuccess)
        {
            _ = channel.DeleteMessageWithTimeoutAsync(msg.Id);
        }
    }

    private async Task<bool> DispatchSubCommand(string rawArgs, SocketTextChannel channel)
    {
        var args = Regexs.MatchWhiteChars().Split(rawArgs, 2);
        if (args.Length < 2)
        {
            await channel.SendCardSafeAsync(HelpMessage);
            return true;
        }

        var maybeId = args.First();
        if (!long.TryParse(maybeId, out var id))
        {
            await channel.SendCardSafeAsync(HelpMessage);
            return true;
        }

        var furtherArg = args[1];
        if (furtherArg.StartsWith("开始日期"))
            return await _service.SetStartDate(channel, id, furtherArg[4..].TrimStart());
        if (furtherArg.StartsWith("结束日期"))
            return await _service.SetEndDate(channel, id, furtherArg[4..].TrimStart());
        if (furtherArg.StartsWith("达标天数"))
            return await _service.SetCount(channel, id, furtherArg[4..].TrimStart());
        if (furtherArg.StartsWith("合格消息"))
            return await _service.SetQualifiedMessage(channel, id, furtherArg[4..].TrimStart());
        if (furtherArg.StartsWith("给予身份"))
            return await _service.SetQualifiedMessage(channel, id, furtherArg[4..].TrimStart());
        if (furtherArg.StartsWith("允许中断天数"))
            return await _service.SetAllowBreakDays(channel, id, furtherArg[6..].TrimStart());
        if (furtherArg.StartsWith("禁用"))
            return await _service.Disable(channel, id);
        if (furtherArg.StartsWith("启用"))
            return await _service.Enable(channel, id);
        if (furtherArg.StartsWith("满足条件的用户"))
            return await _service.ListQualifiedUsers(channel, id);
        await channel.SendCardSafeAsync(HelpMessage);
        return true;
    }
}
