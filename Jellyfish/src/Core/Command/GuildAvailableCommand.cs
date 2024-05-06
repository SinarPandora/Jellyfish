using Kook.WebSocket;

namespace Jellyfish.Core.Command;

/// <summary>
///     Command active on bot connected to a guild
/// </summary>
public abstract class GuildAvailableCommand : Command
{
    /// <summary>
    ///     Execute command
    /// </summary>
    /// <param name="guild">Current guild</param>
    /// <returns>Command result</returns>
    public abstract Task<CommandResult> Execute(SocketGuild guild);
}
