using Kook;
using Kook.WebSocket;

namespace Jellyfish.Core;

public class EventMatcher
{
    private readonly IMessageCommand[] _messageCommands;
    private readonly ICardActionCommand[] _cardActionCommands;
    private readonly KookSocketClient _client;

    public EventMatcher(
        IMessageCommand[] messageCommand,
        ICardActionCommand[] cardActionCommands,
        KookSocketClient client)
    {
        _messageCommands = messageCommand;
        _cardActionCommands = cardActionCommands;
        _client = client;
    }

    /// <summary>
    ///     Handle message received event
    /// </summary>
    /// <param name="msg">User message</param>
    /// <param name="user">Current user</param>
    /// <param name="channel">Current channel</param>
    public Task OnMessageReceived(SocketMessage msg, SocketGuildUser user, SocketTextChannel channel)
    {
        if (user.Id == _client.CurrentUser.Id)
        {
            return Task.CompletedTask;
        }

        _ = Task.Run(async () =>
        {
            foreach (var command in _messageCommands)
            {
                var result = await command.Execute(msg, user, channel);
                if (result == CommandResult.Done) break;
            }
        });
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Handle card action clicked event
    /// </summary>
    /// <param name="value">Card Action Id</param>
    /// <param name="user">Action user</param>
    /// <param name="message">Cached message object</param>
    /// <param name="channel">Current channel</param>
    public Task OnCardActionClicked(string value, Cacheable<SocketGuildUser, ulong> user,
        Cacheable<IMessage, Guid> message, SocketTextChannel channel)
    {
        if (user.Id == _client.CurrentUser.Id)
        {
            return Task.CompletedTask;
        }

        _ = Task.Run(async () =>
        {
            foreach (var command in _cardActionCommands)
            {
                var result = await command.Execute(value, user, message, channel);
                if (result == CommandResult.Done) break;
            }
        });
        return Task.CompletedTask;
    }
}
