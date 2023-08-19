using Jellyfish.Core;
using Kook.WebSocket;

namespace Jellyfish.Command.TeamPlay;

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
