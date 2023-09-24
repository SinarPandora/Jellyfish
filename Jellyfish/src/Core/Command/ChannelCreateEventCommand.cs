using Kook.WebSocket;

namespace Jellyfish.Core.Command;

/// <summary>
///     Event handler command on channel create
/// </summary>
public abstract class ChannelCreateEventCommand : Command
{
    /// <summary>
    ///     Handle channel create event
    /// </summary>
    /// <param name="channel">New created channel</param>
    /// <returns>Command result, Done will finished the execution chains</returns>
    public abstract Task<CommandResult> Execute(SocketChannel channel);
}
