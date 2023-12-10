using Autofac;
using Jellyfish.Core.Command;
using Jellyfish.Module.Role.Core;
using Kook;
using Kook.WebSocket;

namespace Jellyfish.Core.Kook;

public class KookEventMatcher
{
    private readonly ILogger<KookEventMatcher> _log;

    private readonly Lazy<ulong> _currentUserId;

    private readonly IEnumerable<GuildMessageCommand> _messageCommands;
    private readonly IEnumerable<ButtonActionCommand> _buttonActionCommands;
    private readonly IEnumerable<UserConnectEventCommand> _userConnectEventCommands;
    private readonly IEnumerable<UserDisconnectEventCommand> _userDisconnectEventCommands;
    private readonly IEnumerable<DmcCommand> _dmcCommands;
    private readonly IEnumerable<BotJoinGuildCommand> _botJoinGuildCommands;

    public KookEventMatcher(
        IEnumerable<GuildMessageCommand> messageCommand,
        IEnumerable<ButtonActionCommand> buttonActionCommands,
        IEnumerable<UserConnectEventCommand> userConnectEventCommands,
        IEnumerable<UserDisconnectEventCommand> userDisconnectEventCommands,
        IEnumerable<DmcCommand> dmcCommands,
        IComponentContext provider, ILogger<KookEventMatcher> log,
        IEnumerable<BotJoinGuildCommand> botJoinGuildCommands)
    {
        _messageCommands = messageCommand.Where(c => c.Enabled).ToArray();
        _buttonActionCommands = buttonActionCommands.Where(c => c.Enabled).ToArray();
        _userConnectEventCommands = userConnectEventCommands;
        _userDisconnectEventCommands = userDisconnectEventCommands;
        _dmcCommands = dmcCommands;
        _log = log;
        _botJoinGuildCommands = botJoinGuildCommands;
        _currentUserId = new Lazy<ulong>(() => provider.Resolve<KookSocketClient>().CurrentUser.Id);
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
                if (!user.CanExecute(command)) continue;
                try
                {
                    var result = await command.MatchAndExecute(msg, user, channel);
                    if (result == CommandResult.Done) break;
                }
                catch (Exception e)
                {
                    _log.LogInformation(e, "服务器文字指令 {Name} 执行失败！", command.Name());
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
                    _log.LogInformation(e, "卡片操作 {Name} 执行失败！", command.Name());
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
    public Task OnUserConnected(Cacheable<SocketGuildUser, ulong> user, SocketVoiceChannel channel,
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
                    _log.LogInformation(e, "加入语音频道事件操作 {Name} 执行失败！", command.Name());
                }
            }
        });
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Handle user leave a joined voice channel event
    /// </summary>
    /// <param name="user">Current user</param>
    /// <param name="channel">Target channel</param>
    /// <param name="leaveAt">Leave at</param>
    public Task OnUserDisconnected(Cacheable<SocketGuildUser, ulong> user, SocketVoiceChannel channel,
        DateTimeOffset leaveAt)
    {
        if (channel.Users.All(u => u.Id == _currentUserId.Value)) return Task.CompletedTask;

        _ = Task.Run(async () =>
        {
            foreach (var command in _userDisconnectEventCommands)
            {
                try
                {
                    var result = await command.Execute(user, channel, leaveAt);
                    if (result == CommandResult.Done) break;
                }
                catch (Exception e)
                {
                    _log.LogInformation(e, "离开语音频道事件操作 {Name} 执行失败！", command.Name());
                }
            }
        });
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Handle on DMC message received
    /// </summary>
    /// <param name="msg">Direct message</param>
    /// <param name="user">Message sender</param>
    /// <param name="channel">The DMC</param>
    /// <returns></returns>
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
                    _log.LogInformation(e, "私聊文字指令 {Name} 执行失败！", command.Name());
                }
            }
        });
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Handle on bot join to new guild received
    /// </summary>
    /// <param name="guild">Current guild</param>
    public Task OnBotJoinGuild(SocketGuild guild)
    {
        _ = Task.Run(async () =>
        {
            foreach (var command in _botJoinGuildCommands)
            {
                try
                {
                    var result = await command.Execute(guild);
                    if (result == CommandResult.Done) break;
                }
                catch (Exception e)
                {
                    _log.LogInformation(e, "Bot加入服务器指令 {Name} 执行失败！", command.Name());
                }
            }
        });
        return Task.CompletedTask;
    }
}
