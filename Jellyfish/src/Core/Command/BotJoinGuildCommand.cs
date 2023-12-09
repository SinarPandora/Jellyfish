using Kook.WebSocket;

namespace Jellyfish.Core.Command;

/// <summary>
///     Command active on bot join to a new guild
/// </summary>
public abstract class BotJoinGuildCommand : Command
{
    /// <summary>
    ///     Execute command
    /// </summary>
    /// <param name="guild">Current guild</param>
    /// <returns>Command result</returns>
    public abstract Task<CommandResult> Execute(SocketGuild guild);
}
