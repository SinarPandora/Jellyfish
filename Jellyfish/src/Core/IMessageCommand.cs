using Kook.WebSocket;

namespace Jellyfish.Core;

/// <summary>
///     Message command interface
/// </summary>
public interface IMessageCommand
{
    /// <summary>
    ///     Name of command
    /// </summary>
    /// <returns>Command name</returns>
    string Name();

    /// <summary>
    ///     Execute command
    /// </summary>
    /// <param name="msg">User message object</param>
    /// <param name="user">Sender</param>
    /// <param name="channel">Current Channel</param>
    /// <returns>Command result, Done will finished the execution chains</returns>
    Task<CommandResult> Execute(SocketMessage msg, SocketGuildUser user, SocketTextChannel channel);
}
