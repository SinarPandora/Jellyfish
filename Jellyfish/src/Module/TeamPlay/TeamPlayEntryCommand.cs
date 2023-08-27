using Jellyfish.Core.Command;
using Kook.WebSocket;

namespace Jellyfish.Module.TeamPlay;

/// <summary>
///     Team play entry command
/// </summary>
public class TeamPlayEntryCommand : IMessageCommand
{
    private readonly TeamPlayUserAction _userAction;

    public TeamPlayEntryCommand(TeamPlayUserAction userAction)
    {
        _userAction = userAction;
    }

    public string Name()
    {
        return "组队游戏指令";
    }

    public async Task<CommandResult> Execute(SocketMessage msg, SocketGuildUser user, SocketTextChannel channel)
    {
        var content = msg.Content.Trim();

        // User commands
        if (content.StartsWith("/组队"))
            return await HandleUserCommand(msg, user, channel, content[3..].TrimStart());

        // Manager commands
        if (content.StartsWith("！组队") || content.StartsWith("!组队"))
            return await HandleManagerCommand(channel, content[3..].TrimStart());

        return CommandResult.Continue;
    }

    private async Task<CommandResult> HandleUserCommand(SocketMessage raw, SocketGuildUser user,
        SocketTextChannel channel, string content)
    {
        if (content.StartsWith("帮助"))
            await _userAction.Help(raw, user, channel);
        else if (content.StartsWith("人数"))
            await _userAction.SetMemberLimit(raw, user, channel, content[2..].Trim());
        else await _userAction.CreateRoom(raw, user, channel, content);

        return CommandResult.Done;
    }

    private async Task<CommandResult> HandleManagerCommand(SocketTextChannel channel, string content)
    {
        if (content.StartsWith("帮助"))
            await TeamPlayManagerAction.Help(channel);
        else if (content.StartsWith("绑定"))
            await TeamPlayManagerAction.StartBindingParentChannel(channel, content[2..].TrimStart());
        else if (content.StartsWith("绑定文字频道"))
            await TeamPlayManagerAction.StartBindingParentChannel(channel, content[6..].TrimStart());
        else if (content.StartsWith("语音质量"))
            await TeamPlayManagerAction.BindingTextChannel(channel, content[6..].Trim());
        else if (content.StartsWith("列表"))
            await TeamPlayManagerAction.ListConfigs(channel);
        else await TeamPlayManagerAction.Help(channel);

        return CommandResult.Done;
    }
}
