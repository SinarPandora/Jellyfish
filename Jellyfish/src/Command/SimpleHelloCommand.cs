using Jellyfish.Core;
using Jellyfish.Loader;
using Kook;
using Kook.WebSocket;

namespace Jellyfish.Command;

public class SimpleHelloCommand : IMessageCommand
{
    private readonly KookApiFactory _apiFactory;

    public SimpleHelloCommand(KookApiFactory apiFactory = null!)
    {
        _apiFactory = apiFactory ?? throw new ArgumentNullException(nameof(apiFactory));
    }

    public async Task<CommandResult> Execute(SocketMessage msg, SocketGuildUser user, SocketTextChannel channel)
    {
        if (msg is not { Type: MessageType.KMarkdown, Content: "Hello" }) return CommandResult.Done;

        using (var api = await _apiFactory.CreateApiClient())
        {
            Console.WriteLine(api.CurrentUser.Username);
        }

        await channel.SendTextAsync("Simple hello world!");

        return CommandResult.Done;
    }
}
