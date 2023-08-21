using Jellyfish.Core;
using Jellyfish.Loader;
using Kook;
using Kook.WebSocket;

namespace Jellyfish.Command;

public class SimpleHelloCommand : IMessageCommand
{
    private readonly KookApiFactory _apiFactory;

    public SimpleHelloCommand(KookApiFactory apiFactory)
    {
        _apiFactory = apiFactory;
    }

    public async Task<CommandResult> Execute(SocketMessage msg, SocketGuildUser user, SocketTextChannel channel)
    {
        if (msg is not { Type: MessageType.KMarkdown, Content: "Hello" }) return CommandResult.Continue;

        await channel.SendTextAsync("Simple hello world!");

        using var api = _apiFactory.CreateApiClient().Result;
        Console.WriteLine(api.CurrentUser.Username);

        return CommandResult.Done;
    }
}
