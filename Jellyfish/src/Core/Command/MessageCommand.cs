using Kook.WebSocket;

namespace Jellyfish.Core.Command;

/// <summary>
///     Message command interface
/// </summary>
public abstract class MessageCommand : Command, IHelpCommand
{
    /// <summary>
    ///     Execute command
    /// </summary>
    /// <param name="msg">User message object</param>
    /// <param name="user">Sender</param>
    /// <param name="channel">Current Channel</param>
    /// <returns>Command result, Done will finished the execution chains</returns>
    public abstract Task<CommandResult> Execute(SocketMessage msg, SocketGuildUser user, SocketTextChannel channel);

    /// <summary>
    ///     Command keywords, usually what start of each command
    /// </summary>
    /// <returns>Command keywords</returns>
    public abstract string[] Keywords();

    /// <inheritdoc cref="IHelpCommand.Help"/>
    public abstract string Help();
}
