using Jellyfish.Core.Command;
using Jellyfish.Module.Push.Weibo.Core;
using Jellyfish.Util;
using Kook.WebSocket;

namespace Jellyfish.Module.Push.Weibo;

/// <summary>
///     Command for managing Weibo Push jobs
/// </summary>
public class WeiboPushManageCommand : GuildMessageCommand
{
    private readonly WeiboPushManageService _service;

    public WeiboPushManageCommand(WeiboPushManageService service) : base(true)
    {
        _service = service;
        HelpMessage = HelpMessageHelper.ForMessageCommand(this,
            """
            管理当前服务器内的微博推送
            ---
            微博动态每三分钟扫描一次，若检测到有新微博，将转发到指定频道
            """,
            """
            [微博用户UID]：以下简称[UID]，是微博用户的用户 ID，显示于浏览器地址中的最后一个斜线后方：
            如用户主页的浏览器地址为：https://weibo.com/u/1234567890
            他/她的 UID 为：1234567890
            ---
            基础功能：
            1. `添加 [UID/别名] [#引用现有文字频道]`：定期向指定频道推送指定用户微博，第一次添加时必须使用 UID
            2. `别名 [UID] [别名]`：给对应微博用户起别名，可以在相关指令中使用
            3. `删除 [UID/别名] [#引用现有文字频道]`：取消对应用户在对应频道的微博推送
            4. `删除 [UID/别名]`：取消对应用户在所有频道的微博推送
            5. `列表`：列出全部推送信息
            ---
            [#引用现有文字频道]：指的是一个文字频道的 Kook 引用（在软件中为蓝色文字），用于将消息推送到指定频道）
            """);
    }

    public override string Name() => "微博推送管理指令";

    public override IEnumerable<string> Keywords() => ["！微博推送", "!微博推送"];

    protected override async Task Execute(string args, string keyword, SocketMessage msg, SocketGuildUser user,
        SocketTextChannel channel)
    {
        if (args.StartsWith(HelpMessageHelper.HelpCommand))
        {
            await channel.SendCardSafeAsync(HelpMessage);
            return;
        }

        var isSuccess = true;
        if (args.StartsWith("添加"))
            isSuccess = await _service.CreateOrAppendPushConfig(channel, args[2..].TrimStart());
        else if (args.StartsWith("别名"))
            isSuccess = await _service.RenameConfig(channel, args[2..].TrimStart());
        else if (args.StartsWith("删除"))
            isSuccess = await _service.RemovePushInstance(channel, args[2..].TrimStart());
        else if (args.StartsWith("列表"))
            await _service.ListPushConfig(channel);
        else
            await channel.SendCardSafeAsync(HelpMessage);

        if (!isSuccess)
        {
            _ = channel.DeleteMessageWithTimeoutAsync(msg.Id);
        }
    }
}
