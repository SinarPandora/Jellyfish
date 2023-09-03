using Kook.WebSocket;

namespace Jellyfish.Core.Command;

/// <summary>
///     Message command interface
/// </summary>
public abstract class MessageCommand : Command
{
    public string HelpMessage { get; protected init; } = "该指令不包含帮助信息";

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
    /// <returns>Command result, Done will finished the execution chains</returns>
    public async Task<CommandResult> MatchAndExecute(SocketMessage msg, SocketGuildUser user, SocketTextChannel channel)
    {
        var keyword = Keywords().FirstOrDefault(k => msg.Content.StartsWith(k));
        if (keyword == null) return CommandResult.Continue;
        await Execute(msg.Content[keyword.Length..].Trim(), msg, user, channel);
        return CommandResult.Done;
    }

    /// <summary>
    ///     Execute command
    /// </summary>
    /// <param name="args">Command args</param>
    /// <param name="msg">User message object</param>
    /// <param name="user">Sender</param>
    /// <param name="channel">Current Channel</param>
    public abstract Task Execute(string args, SocketMessage msg, SocketGuildUser user, SocketTextChannel channel);
}
