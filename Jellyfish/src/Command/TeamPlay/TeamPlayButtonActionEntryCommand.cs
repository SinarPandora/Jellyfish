using Jellyfish.Core;
using Kook;
using Kook.WebSocket;

namespace Jellyfish.Command.TeamPlay;

/// <summary>
///     Team play card action
/// </summary>
public class TeamPlayButtonActionEntryCommand : IButtonActionCommand
{
    private readonly TeamPlayManagerAction _managerAction;

    public TeamPlayButtonActionEntryCommand(TeamPlayManagerAction managerAction)
    {
        _managerAction = managerAction;
    }

    public string Name()
    {
        return "组队游戏卡片操作";
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
