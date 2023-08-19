using AutoDI;
using Jellyfish.Core;
using Jellyfish.Loader;
using JetBrains.Annotations;
using Kook;
using Kook.WebSocket;

namespace Jellyfish.Command;

[UsedImplicitly]
public class SimpleHelloCommand : IMessageCommand
{
    private readonly KookLoader _kookLoader;

    public SimpleHelloCommand([Dependency] KookLoader kookLoader = null!)
    {
        _kookLoader = kookLoader ?? throw new ArgumentNullException(nameof(kookLoader));
    }

    public async Task<CommandResult> Execute(SocketMessage msg, SocketGuildUser user, SocketTextChannel channel)
    {
        if (msg is not { Type: MessageType.KMarkdown, Content: "Hello" }) return CommandResult.Done;

        using (var api = await _kookLoader.CreateApiClient())
        {
            Console.WriteLine(api.CurrentUser.Username);
        }

        await channel.SendTextAsync("Simple hello world!");

        return CommandResult.Done;
    }
}
