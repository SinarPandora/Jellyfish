using Jellyfish.Core;
using Kook;
using Kook.WebSocket;

namespace Jellyfish.Command.TeamPlay;

/// <summary>
///     Team play card action
/// </summary>
public class TeamPlayCardActionEntryCommand : ICardActionCommand
{
    private readonly TeamPlayManagerAction _managerAction;

    public TeamPlayCardActionEntryCommand(TeamPlayManagerAction managerAction)
    {
        _managerAction = managerAction;
    }

    public async Task<CommandResult> Execute(string value, Cacheable<SocketGuildUser, ulong> user,
        Cacheable<IMessage, Guid> message, SocketTextChannel channel)
    {
        if (value.StartsWith("tp_binding_"))
        {
            await _managerAction.DoBindingParentChannel(value[11..], user, message, channel);
            return CommandResult.Done;
        }

        return CommandResult.Continue;
    }
}
