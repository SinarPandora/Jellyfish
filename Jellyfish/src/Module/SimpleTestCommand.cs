using Jellyfish.Core.Command;
using Jellyfish.Core.Kook;
using Kook;
using Kook.WebSocket;
using NLog;

namespace Jellyfish.Module;

/// <summary>
///     Simple testing command
/// </summary>
public class SimpleTestCommand : IMessageCommand
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly KookApiFactory _apiFactory;

    public SimpleTestCommand(KookApiFactory apiFactory)
    {
        _apiFactory = apiFactory;
    }

    public string Name()
    {
        return "简单测试指令";
    }

    public async Task<CommandResult> Execute(SocketMessage msg, SocketGuildUser user, SocketTextChannel channel)
    {
        if (msg is not { Type: MessageType.KMarkdown, Content: "！Test" }) return CommandResult.Continue;

        await channel.SendTextAsync("Simple hello world!");

        Log.Info($"Current Boot Level is {channel.Guild.BoostLevel}");

        return CommandResult.Done;
    }
}
