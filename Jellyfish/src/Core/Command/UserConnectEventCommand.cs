using Kook;
using Kook.WebSocket;

namespace Jellyfish.Core.Command;

/// <summary>
///     Event handler command on user connect any voice channel
/// </summary>
public abstract class UserConnectEventCommand : Command
{
    /// <summary>
    ///     Handle user connect to voice channel event
    /// </summary>
    /// <param name="user">Current user</param>
    /// <param name="channel">Target channel</param>
    /// <param name="joinAt">Join at</param>
    /// <returns>Command result, Done will finished the execution chains</returns>
    public abstract Task<CommandResult> Execute(
        Cacheable<SocketGuildUser, ulong> user,
        SocketVoiceChannel channel,
        DateTimeOffset joinAt
    );
}
