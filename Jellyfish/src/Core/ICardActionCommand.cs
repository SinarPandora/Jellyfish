using Kook;
using Kook.WebSocket;

namespace Jellyfish.Core;

/// <summary>
///     Card action command interface
/// </summary>
public interface ICardActionCommand
{
    /// <summary>
    ///     Execute command
    /// </summary>
    /// <param name="value">Card Action Id</param>
    /// <param name="user">Action user</param>
    /// <param name="message">Cached message object</param>
    /// <param name="channel">Current channel</param>
    /// <returns>Command result, Done will finished the execution chains</returns>
    Task<CommandResult> Execute(string value, Cacheable<SocketGuildUser, ulong> user, Cacheable<IMessage, Guid> message,
        SocketTextChannel channel);
}
