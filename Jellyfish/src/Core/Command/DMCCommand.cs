using Kook.WebSocket;

namespace Jellyfish.Core.Command;

/// <summary>
///     DMC message command
/// </summary>
public abstract class DmcCommand : Command
{
    /// <summary>
    ///     Command keywords, usually what start of each command
    /// </summary>
    /// <returns>Command keywords</returns>
    protected abstract IEnumerable<string> Keywords();

    /// <summary>
    ///     Match and then execute command
    /// </summary>
    /// <param name="msg">User message object</param>
    /// <param name="user">Sender</param>
    /// <param name="channel">Current Channel</param>
    /// <returns>Command result, Done will finished the execution chains</returns>
    public async Task<CommandResult> MatchAndExecute(SocketMessage msg, SocketUser user, SocketDMChannel channel)
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
    protected abstract Task Execute(string args, string keyword, SocketMessage msg, SocketUser user,
        SocketDMChannel channel);
}
