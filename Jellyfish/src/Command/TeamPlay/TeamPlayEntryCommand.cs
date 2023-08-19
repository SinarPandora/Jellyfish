using Jellyfish.Core;
using JetBrains.Annotations;
using Kook.WebSocket;

namespace Jellyfish.Command.TeamPlay;

/// <summary>
///     Team play entry command
/// </summary>
[UsedImplicitly]
public class TeamPlayEntryCommand : IMessageCommand
{
    public async Task<CommandResult> Execute(SocketMessage msg, SocketGuildUser user, SocketTextChannel channel)
    {
        var content = msg.Content;

        // User commands
        if (content.StartsWith("/组队"))
        {
            // content remove prefix
            content = content[3..];

            return CommandResult.Continue;
        }

        // Manager commands
        if (content.StartsWith("！组队")) return CommandResult.Done;

        return CommandResult.Continue;
    }
}
