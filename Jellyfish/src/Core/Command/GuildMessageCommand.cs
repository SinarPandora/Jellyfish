using Kook;
using Kook.WebSocket;

namespace Jellyfish.Core.Command;

/// <summary>
///     Guild message command interface
/// </summary>
public abstract class GuildMessageCommand(bool isManagerCommand) : Command
{
    /// <summary>
    ///     Mark as manager command will be limited by default manager account and role
    /// </summary>
    public bool IsManagerCommand { get; } = isManagerCommand;

    private static readonly Card EmptyHelp = new CardBuilder()
        .AddModule<SectionModuleBuilder>(m => m.WithText("该指令没有帮助信息"))
        .Build();

    protected Card HelpMessage { get; init; } = EmptyHelp;

    /// <summary>
    ///     Command keywords, usually what start of each command
    /// </summary>
    /// <returns>Command keywords</returns>
    public abstract IEnumerable<string> Keywords();

    /// <summary>
    ///     Match and then execute command
    /// </summary>
    /// <param name="msg">User message object</param>
    /// <param name="user">Sender</param>
    /// <param name="channel">Current Channel</param>
    /// <returns>Command result, Done will finish the execution chains</returns>
    public async Task<CommandResult> MatchAndExecute(SocketMessage msg, SocketGuildUser user, SocketTextChannel channel)
    {
        var keyword = Keywords().FirstOrDefault(k => msg.Content.StartsWith(k));
        if (keyword is null) return CommandResult.Continue;
        await Execute(msg.Content[keyword.Length..].Trim(), keyword, msg, user, channel);
        return CommandResult.Done;
    }

    /// <summary>
    ///     Execute command
    /// </summary>
    /// <param name="args">Command args</param>
    /// <param name="keyword">Matched keyword</param>
    /// <param name="msg">User message object</param>
    /// <param name="user">Sender</param>
    /// <param name="channel">Current Channel</param>
    protected abstract Task Execute(string args, string keyword, SocketMessage msg, SocketGuildUser user,
        SocketTextChannel channel);
}
