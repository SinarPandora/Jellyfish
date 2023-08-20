using Jellyfish.Command;
using Jellyfish.Command.TeamPlay;
using Kook.WebSocket;

namespace Jellyfish.Core;

public class EventMatcher
{
    private readonly List<IMessageCommand> _commands = new();

    public EventMatcher(
        SimpleHelloCommand simpleHelloCommand,
        TeamPlayEntryCommand teamPlayEntryCommand
    )
    {
        _commands.Add(simpleHelloCommand);
        _commands.Add(teamPlayEntryCommand);
    }

    /// <summary>
    ///     Handle message received event
    /// </summary>
    /// <param name="msg">User message</param>
    /// <param name="user">Current user</param>
    /// <param name="channel">Current channel</param>
    public async Task OnMessageReceived(SocketMessage msg, SocketGuildUser user, SocketTextChannel channel)
    {
        foreach (var command in _commands)
        {
            var result = await command.Execute(msg, user, channel);
            if (result == CommandResult.Done) break;
        }
    }
}
