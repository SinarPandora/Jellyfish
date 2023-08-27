using Jellyfish.Core.Command;
using Kook;
using Kook.WebSocket;

namespace Jellyfish.Module.TeamPlay;

/// <summary>
///     Team play card action
/// </summary>
public class TeamPlayButtonActionEntry : IButtonActionCommand
{
    public string Name()
    {
        return "组队游戏卡片操作";
    }

    public async Task<CommandResult> Execute(string value, Cacheable<SocketGuildUser, ulong> user,
        Cacheable<IMessage, Guid> message, SocketTextChannel channel)
    {
        if (value.StartsWith("tp_binding_"))
        {
            await TeamPlayManagerAction.DoBindingParentChannel(value[11..], user, channel);
            return CommandResult.Done;
        }

        return CommandResult.Continue;
    }
}
