using Jellyfish.Core;
using JetBrains.Annotations;
using Kook.WebSocket;

namespace Jellyfish.Command.TeamPlay;

[UsedImplicitly]
public class TeamPlayUserAction
{
    public async Task<CommandResult> Help(SocketMessage msg, SocketGuildUser user, SocketTextChannel channel)
    {
        throw new NotImplementedException();
    }


    public async Task<CommandResult> CreateRoom(SocketMessage raw, SocketGuildUser user, SocketTextChannel channel,
        string msg)
    {
        throw new NotImplementedException();
    }

    public async Task<CommandResult> SetMemberLimit(SocketMessage raw, SocketGuildUser user, SocketTextChannel channel,
        string msg)
    {
        throw new NotImplementedException();
    }
}
