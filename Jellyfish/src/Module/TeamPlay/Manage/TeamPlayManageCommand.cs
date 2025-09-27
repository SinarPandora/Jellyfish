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
    private readonly TeamPlayManageService _service;

    public TeamPlayManageCommand(TeamPlayManageService service) : base(true)
    {
        _service = service;
        HelpMessage = HelpMessageHelper.ForMessageCommand(this,
            """
            管理组队配置
            ---
            您可以绑定语音入口频道，该频道将成为后续自动创建语音频道的入口
            您也可以绑定任意文字频道为入口频道，在目标频道发送由 /组队 开头的消息将自动创建对应房间
            当绑定了一个语音入口频道或文字入口频道后，配置就可以使用啦
            """,
            $"""
             1. 列表：列出全部的组队配置
             2. 配置 [配置名称]：调整指定组队配置
             3. 绑定文字频道 [配置名称] [#引用现有文字频道]：设置后，该频道发送的组队指令会使用该配置创建语音频道
             3. 通知文字频道 [配置名称] [#引用现有文字频道]：设置后，通过语音频道自动创建的房间将会向该频道发送通知（若未设置则使用指令文字频道）
             4. 语音频道分组 [配置名称] [#引用现有文字频道]：设置临时语音房间在指定分组下创建
             5. 文字频道分组 [配置名称] [#引用现有文字频道]：设置临时文字房间在指定分组下创建
             4. 房间名格式 [配置名称] [名称格式]：修改语音房间名称格式，使用 {TeamPlayManageService.UserInjectKeyword} 代表用户输入的内容
             5. 默认人数 [配置名称] [数字]：设定创建语音房间的默认人数，输入 0 代表人数无限
             6. 关闭临时文字房间 [配置名称]：关闭后创建语音房间时将不会同时创建临时文字房间
             7. 开启临时文字房间 [配置名称]：开启临时文字房间功能
             8. 删除 [配置名称]：删除指定配置
             ---
             [#引用现有文字频道]：指的是一个文字频道的 Kook 引用，用于获取其所属的分类频道（因为 Kook 无法直接引用分类频道）
             """);
    }

    public override string Name() => "管理组队配置指令";

    public override IEnumerable<string> Keywords() => ["!组队", "！组队"];

    protected override async Task Execute(string args, string keyword, SocketMessage msg, SocketGuildUser user,
        SocketTextChannel channel)
    {
        if (args.StartsWith(HelpMessageHelper.HelpCommand))
        {
            await channel.SendCardSafeAsync(HelpMessage);
            return;
        }

        var isSuccess = true;
        if (args.StartsWith("配置"))
            isSuccess = await _service.SendBindingWizard(user, channel, args[2..].TrimStart());
        else if (args.StartsWith("绑定文字频道"))
            isSuccess = await _service.BindingTextChannel(channel, args[6..].TrimStart());
        else if (args.StartsWith("通知文字频道"))
            isSuccess = await _service.SetCategoryChannel(channel, args[6..].TrimStart(),
                AdditionChannelType.CreationNotify);
        else if (args.StartsWith("房间名格式"))
            isSuccess = await _service.SetRoomPattern(channel, args[5..].TrimStart());
        else if (args.StartsWith("默认人数"))
            isSuccess = await _service.SetDefaultMemberCount(channel, args[4..].TrimStart());
        else if (args.StartsWith("删除"))
            isSuccess = await _service.RemoveConfig(channel, args[2..].TrimStart());
        else if (args.StartsWith("语音频道分组"))
            isSuccess = await _service.SetCategoryChannel(channel, args[6..].TrimStart(),
                AdditionChannelType.TmpVoiceCategoryInto);
        else if (args.StartsWith("文字频道分组"))
            isSuccess = await _service.SetCategoryChannel(channel, args[6..].TrimStart(),
                AdditionChannelType.TmpTextCategoryInto);
        else if (args.StartsWith("关闭临时文字房间"))
            isSuccess = await _service.SetTmpTextChannelEnabled(channel, args[8..].TrimStart(), false);
        else if (args.StartsWith("开启临时文字房间"))
            isSuccess = await _service.SetTmpTextChannelEnabled(channel, args[8..].TrimStart(), true);
        else if (args.StartsWith("列表"))
            await _service.ListConfigs(channel);
        else
            await channel.SendCardSafeAsync(HelpMessage);

        if (!isSuccess)
        {
            _ = channel.DeleteMessageWithTimeoutAsync(msg.Id);
        }
    }
}
