using Jellyfish.Core.Cache;
using Jellyfish.Core.Command;
using Kook;
using Kook.WebSocket;
using NLog;

namespace Jellyfish.Core.Kook;

public class EventMatcher
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly ButtonActionCommand[] _buttonActionCommands;
    private readonly KookSocketClient _client;

    private readonly MessageCommand[] _messageCommands;

    public EventMatcher(
        MessageCommand[] messageCommand,
        ButtonActionCommand[] buttonActionCommands,
        KookSocketClient client)
    {
        _messageCommands = messageCommand.FindAll(c => c.Enabled).ToArray();
        _buttonActionCommands = buttonActionCommands.FindAll(c => c.Enabled).ToArray();
        ;
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
            {
                if (!CheckIfUserHasPermission(user, command.Name())) continue;
                try
                {
                    var result = await command.Execute(msg, user, channel);
                    if (result == CommandResult.Done) break;
                }
                catch (Exception e)
                {
                    Log.Info(e, $"指令 {command.Name()} 执行失败！");
                }
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
            {
                if (!CheckIfUserHasPermission(user.Value, command.Name())) continue;
                try
                {
                    var result = await command.Execute(value, user, message, channel);
                    if (result == CommandResult.Done) break;
                }
                catch (Exception e)
                {
                    Log.Info(e, $"卡片操作 {command.Name()} 执行失败！");
                }
            }
        });
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Check if current user has permission to perform command
    /// </summary>
    /// <param name="user">Kook user</param>
    /// <param name="commandName">Command name</param>
    /// <returns>Does user has permission or not</returns>
    private static bool CheckIfUserHasPermission(SocketGuildUser user, string commandName)
    {
        var permissions = AppCaches.Permissions.Get($"{user.Guild.Id}_{commandName}");
        return permissions == null || permissions.ContainsAny(user.Roles.Select(it => it.Id).ToArray());
    }
}
