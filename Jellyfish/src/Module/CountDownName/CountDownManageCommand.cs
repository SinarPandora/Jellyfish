using Jellyfish.Core.Command;
using Jellyfish.Module.CountDownName.Core;
using Jellyfish.Util;
using Kook.WebSocket;

namespace Jellyfish.Module.CountDownName;

/// <summary>
///     CountDown-Name channel management command
/// </summary>
public class CountDownManageCommand : GuildMessageCommand
{
    private readonly CountDownChannelService _service;

    public CountDownManageCommand(CountDownChannelService service) : base(true)
    {
        _service = service;
        HelpMessage = HelpMessageHelper.ForMessageCommand(this,
            """
            频道名称倒/正计时配置指令

            将频道的名称当成倒计时使用（也可以正计时）
            如：距离劳动节还有1️⃣2️⃣天
            每天零点，Bot 会自动更新倒计时。
            """,
            """
            1. 列表：列出已配置的倒/正计时
            2. 创建 [频道引用] [日期（年-月-日）] [名称格式]：创建倒计时，如果日期为当天或以前则为正计时
            3. 删除 [编号/频道引用]：删除指定正/倒计时
            4. 到期名称 [编号/频道引用] [到期频道名称]：设置到达指定日期时显示的频道标题
            5. 到期名称 删除 [编号/频道引用]：删除到达指定日期时显示的频道标题

            其中：
            日期格式为：四位年份-两位月份-两位日期，如 2024-05-01，代表 2024 年 5 月 1 日（月份和日期中占位的 0 可以省略）
            在名称格式中添加 `{COUNT}` 占位符，代表距离到期的天数

            举例：
            使用 `！频道倒计时 创建 #频道 2024-05-01 距离劳动节还有{COUNT}天`
            将设置频道名称为：距离劳动节还有1️⃣2️⃣天
            """);
    }

    public override string Name() => "频道名称倒计时配置指令";

    public override IEnumerable<string> Keywords() => ["！频道倒计时", "!频道倒计时", "！倒计时频道", "!倒计时频道"];

    protected override async Task Execute(string args, string keyword, SocketMessage msg, SocketGuildUser user,
        SocketTextChannel channel)
    {
        if (args.StartsWith(HelpMessageHelper.HelpCommand))
        {
            await channel.SendCardSafeAsync(HelpMessage);
            return;
        }

        var isSuccess = true;
        if (args.StartsWith("列表"))
            isSuccess = await _service.List(channel);
        else if (args.StartsWith("创建"))
            isSuccess = await _service.ParseAndCreate(args[2..].TrimStart(), channel);
        else if (args.StartsWith("删除"))
            isSuccess = await _service.Delete(args[2..].TrimStart(), channel);
        else if (args.StartsWith("到期名称"))
        {
            var rawArgs = args[4..].TrimStart();
            if (rawArgs.StartsWith("删除"))
                isSuccess = await _service.RemoveDueText(rawArgs[2..].TrimStart(), channel);
            else
                isSuccess = await _service.PersistDueText(rawArgs, channel);
        }
        else
            await channel.SendCardSafeAsync(HelpMessage);

        if (!isSuccess)
        {
            _ = channel.DeleteMessageWithTimeoutAsync(msg.Id);
        }
    }
}
