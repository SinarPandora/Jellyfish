using Jellyfish.Core.Cache;
using Jellyfish.Core.Command;
using Kook;
using Kook.WebSocket;
using Ninject;
using NLog;
using AppContext = Jellyfish.Core.Container.AppContext;

namespace Jellyfish.Core.Kook;

public class EventMatcher
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly Lazy<ulong> _currentUserId = new(
        () => AppContext.Instance.Get<KookSocketClient>().CurrentUser.Id
    );

    private readonly GuildMessageCommand[] _messageCommands;
    private readonly ButtonActionCommand[] _buttonActionCommands;
    private readonly UserConnectEventCommand[] _userConnectEventCommands;
    private readonly DmcCommand[] _dmcCommands;

    public EventMatcher(
        GuildMessageCommand[] messageCommand,
        ButtonActionCommand[] buttonActionCommands,
        UserConnectEventCommand[] userConnectEventCommands,
        DmcCommand[] dmcCommands)
    {
        _messageCommands = messageCommand.FindAll(c => c.Enabled).ToArray();
        _buttonActionCommands = buttonActionCommands.FindAll(c => c.Enabled).ToArray();
        _userConnectEventCommands = userConnectEventCommands;
        _dmcCommands = dmcCommands;
    }

    /// <summary>
    ///     Handle message received event
    /// </summary>
    /// <param name="msg">User message</param>
    /// <param name="user">Current user</param>
    /// <param name="channel">Current channel</param>
    public Task OnMessageReceived(SocketMessage msg, SocketGuildUser user, SocketTextChannel channel)
    {
        if (user.Id == _currentUserId.Value) return Task.CompletedTask;

        _ = Task.Run(async () =>
        {
            foreach (var command in _messageCommands)
            {
                if (!CheckIfUserHasPermission(user, command.Name())) continue;
                try
                {
                    var result = await command.MatchAndExecute(msg, user, channel);
                    if (result == CommandResult.Done) break;
                }
                catch (Exception e)
                {
                    Log.Info(e, $"服务器文字指令 {command.Name()} 执行失败！");
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
    /// <param name="msg">Cached message object</param>
    /// <param name="channel">Current channel</param>
    public Task OnCardActionClicked(string value, Cacheable<SocketGuildUser, ulong> user,
        Cacheable<IMessage, Guid> msg, SocketTextChannel channel)
    {
        if (user.Id == _currentUserId.Value) return Task.CompletedTask;

        _ = Task.Run(async () =>
        {
            foreach (var command in _buttonActionCommands)
            {
                try
                {
                    var result = await command.Execute(value, user, msg, channel);
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
    ///     Handle user connect to voice channel event
    /// </summary>
    /// <param name="user">Current user</param>
    /// <param name="channel">Target channel</param>
    /// <param name="joinAt">Join at</param>
    public Task OnChannelCreated(Cacheable<SocketGuildUser, ulong> user, SocketVoiceChannel channel,
        DateTimeOffset joinAt)
    {
        if (channel.Users.All(u => u.Id == _currentUserId.Value)) return Task.CompletedTask;

        _ = Task.Run(async () =>
        {
            foreach (var command in _userConnectEventCommands)
            {
                try
                {
                    var result = await command.Execute(user, channel, joinAt);
                    if (result == CommandResult.Done) break;
                }
                catch (Exception e)
                {
                    Log.Info(e, $"频道创建事件操作 {command.Name()} 执行失败！");
                }
            }
        });
        return Task.CompletedTask;
    }

    public Task OnDirectMessageReceived(SocketMessage msg, SocketUser user, SocketDMChannel channel)
    {
        if (user.Id == _currentUserId.Value) return Task.CompletedTask;

        _ = Task.Run(async () =>
        {
            foreach (var command in _dmcCommands)
            {
                try
                {
                    var result = await command.MatchAndExecute(msg, user, channel);
                    if (result == CommandResult.Done) break;
                }
                catch (Exception e)
                {
                    Log.Info(e, $"私聊文字指令 {command.Name()} 执行失败！");
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
        return !AppCaches.Permissions.ContainsKey($"{user.Guild.Id}_{commandName}")
               || AppCaches.Permissions.GetValueOrDefault($"{user.Guild.Id}_{commandName}")
                   .ContainsAny(user.Roles.Select(it => it.Id).ToArray());
    }
}
