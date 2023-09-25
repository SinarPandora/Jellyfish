using Kook;
using Kook.WebSocket;

namespace Jellyfish.Core.Command;

/// <summary>
///     Event handler command on user disconnect from any voice channel
/// </summary>
public abstract class UserDisconnectEventCommand : Command
{
    /// <summary>
    ///     Handle user connect to voice channel event
    /// </summary>
    /// <param name="user">Current user</param>
    /// <param name="channel">Target channel</param>
    /// <param name="leaveAt">Leave at</param>
    /// <returns>Command result, Done will finished the execution chains</returns>
    public abstract Task<CommandResult> Execute(Cacheable<SocketGuildUser, ulong> user, SocketVoiceChannel channel,
        DateTimeOffset leaveAt);
}
