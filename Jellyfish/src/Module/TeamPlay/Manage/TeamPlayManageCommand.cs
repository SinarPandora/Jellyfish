using Jellyfish.Core.Command;
using Jellyfish.Module.TeamPlay.Core;
using Jellyfish.Util;
using Kook.WebSocket;

namespace Jellyfish.Module.TeamPlay.Manage;

/// <summary>
///     Team play config manage command
/// </summary>
public class TeamPlayManageCommand : GuildMessageCommand
{
    public TeamPlayManageCommand()
    {
        HelpMessage = HelpMessageTemplate.ForMessageCommand(this,
            """
            管理组队配置

            您可以绑定语音入口频道，该频道将成为后续自动创建语音频道的入口
            您也可以绑定任意文字频道为入口频道，在目标频道发送由 /组队 开头的消息将自动创建对应房间
            当绑定了一个语音入口频道或文字入口频道后，配置就可以使用啦
            """,
            $"""
             - 列表：列出全部的组队配置
             - 配置 [配置名称]：调整指定组队配置
             - 绑定文字频道 [配置名称]：在目标频道中使用，设置后，该频道发送的组队质量会使用该配置创建语音频道
             - 房间名格式 [配置名称] [名称格式]：修改语音房间名称格式，使用 {TeamPlayManageService.UserInjectKeyword} 代表用户输入的内容
             - 默认人数 [配置名称] [数字]：设定创建语音房间的默认人数，输入 0 代表人数无限
             - 语音质量 [配置名称] [低|中|高]：设定临时语音频道的质量
             - 删除 [配置名称]：删除指定配置
             """);
    }

    public override string Name() => "管理组队配置指令";

    public override IEnumerable<string> Keywords() => new[] { "!组队", "！组队" };

    protected override async Task Execute(string args, string keyword, SocketMessage msg, SocketGuildUser user,
        SocketTextChannel channel)
    {
        if (args.StartsWith("帮助"))
            await channel.SendTextAsync(HelpMessage);
        else if (args.StartsWith("配置"))
            await TeamPlayManageService.SendBindingWizard(user, channel, args[2..].TrimStart());
        else if (args.StartsWith("绑定文字频道"))
            await TeamPlayManageService.BindingTextChannel(channel, args[6..].TrimStart());
        else if (args.StartsWith("语音质量"))
            await TeamPlayManageService.SetDefaultQuality(channel, args[4..].TrimStart());
        else if (args.StartsWith("房间名格式"))
            await TeamPlayManageService.SetRoomPattern(channel, args[5..].TrimStart());
        else if (args.StartsWith("默认人数"))
            await TeamPlayManageService.SetDefaultMemberCount(channel, args[4..].TrimStart());
        else if (args.StartsWith("删除"))
            await TeamPlayManageService.RemoveConfig(channel, args[2..].TrimStart());
        else if (args.StartsWith("列表"))
            await TeamPlayManageService.ListConfigs(channel);
        else
            await channel.SendTextAsync(HelpMessage);
    }
}
