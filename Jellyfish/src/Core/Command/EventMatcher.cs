using Kook;
using Kook.WebSocket;
using NLog;

namespace Jellyfish.Core.Command;

public class EventMatcher
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly IButtonActionCommand[] _buttonActionCommands;
    private readonly KookSocketClient _client;

    private readonly IMessageCommand[] _messageCommands;

    public EventMatcher(
        IMessageCommand[] messageCommand,
        IButtonActionCommand[] buttonActionCommands,
        KookSocketClient client)
    {
        _messageCommands = messageCommand;
        _buttonActionCommands = buttonActionCommands;
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
        if (user.Id == _client.CurrentUser.Id) return Task.CompletedTask;

        _ = Task.Run(async () =>
        {
            foreach (var command in _messageCommands)
                try
                {
                    var result = await command.Execute(msg, user, channel);
                    if (result == CommandResult.Done) break;
                }
                catch (Exception e)
                {
                    Log.Info(e, $"指令 {command.Name()} 执行失败！");
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
        if (user.Id == _client.CurrentUser.Id) return Task.CompletedTask;

        _ = Task.Run(async () =>
        {
            foreach (var command in _buttonActionCommands)
                try
                {
                    var result = await command.Execute(value, user, message, channel);
                    if (result == CommandResult.Done) break;
                }
                catch (Exception e)
                {
                    Log.Info(e, $"卡片操作 {command.Name()} 执行失败！");
                }
        });
        return Task.CompletedTask;
    }
}
