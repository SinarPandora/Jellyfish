using Jellyfish.Core;
using Kook.WebSocket;

namespace Jellyfish.Command.TeamPlay;

/// <summary>
///     Team play entry command
/// </summary>
public class TeamPlayEntryCommand : IMessageCommand
{
    private readonly TeamPlayUserAction _userAction;
    private readonly TeamPlayManagerAction _managerAction;

    public TeamPlayEntryCommand(
        TeamPlayUserAction? userAction = null,
        TeamPlayManagerAction? managerAction = null
    )
    {
        _userAction = userAction ?? throw new ArgumentNullException(nameof(userAction));
        _managerAction = managerAction ?? throw new ArgumentNullException(nameof(managerAction));
    }

    public async Task<CommandResult> Execute(SocketMessage msg, SocketGuildUser user, SocketTextChannel channel)
    {
        var content = msg.Content.Trim();

        // User commands
        if (content.StartsWith("/组队"))
            return await HandleUserCommand(msg, user, channel, content);

        // Manager commands
        if (content.StartsWith("！组队") || content.StartsWith("!组队"))
            return await HandleManagerCommand(msg, user, channel, content);

        return CommandResult.Continue;
    }

    private async Task<CommandResult> HandleUserCommand(SocketMessage raw, SocketGuildUser user,
        SocketTextChannel channel, string content)
    {
        // content remove prefix
        content = content[3..];

        if (content.StartsWith("帮助"))
        {
            return await _userAction.Help(raw, user, channel);
        }

        if (content.StartsWith("人数"))
        {
            return await _userAction.SetMemberLimit(raw, user, channel, content[2..]);
        }

        return await _userAction.CreateRoom(raw, user, channel, content);
    }

    private async Task<CommandResult> HandleManagerCommand(SocketMessage raw, SocketGuildUser user,
        SocketTextChannel channel, string content)
    {
        // content remove prefix
        content = content[3..];

        if (content.StartsWith("帮助"))
        {
            return await TeamPlayManagerAction.Help(channel);
        }

        if (content.StartsWith("绑定父频道"))
        {
            return await _managerAction.BindingParentChannel(raw, user, channel);
        }

        if (content.StartsWith("默认语音质量"))
        {
            return await _managerAction.SetDefaultQuality(raw, user, channel, content[6..]);
        }

        return CommandResult.Continue;
    }
}
