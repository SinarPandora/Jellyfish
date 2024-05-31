using Jellyfish.Core.Command;
using Kook.WebSocket;

namespace Jellyfish.Module.Board;

/// <summary>
///     Board manage command
/// </summary>
public class BoardManageCommand() : GuildMessageCommand(true)
{
    public override string Name()
    {
        throw new NotImplementedException();
    }

    public override IEnumerable<string> Keywords()
    {
        throw new NotImplementedException();
    }

    protected override Task Execute(string args, string keyword, SocketMessage msg, SocketGuildUser user,
        SocketTextChannel channel)
    {
        throw new NotImplementedException();
    }
}
