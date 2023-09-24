using System.Diagnostics;
using Jellyfish.Core.Command;
using Jellyfish.Core.Kook;
using Kook.WebSocket;
using NLog;

namespace Jellyfish.Module;

/// <summary>
///     Simple testing command
/// </summary>
public class SimpleTestCommand : GuildMessageCommand
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly KookApiFactory _apiFactory;

    public SimpleTestCommand(KookApiFactory apiFactory)
    {
        _apiFactory = apiFactory;
        Enabled = false;
        HelpMessage = "无";
        EnableOnlyOnDebug();
    }

    public override string Name() => "简单测试指令";

    public override string[] Keywords() => new[] { "!Test", "！Test" };

    protected override async Task Execute(string args, string keyword, SocketMessage msg, SocketGuildUser user,
        SocketTextChannel channel)
    {
        await channel.SendTextAsync("Simple hello world!");
        Log.Info($"Current Boot Level is {channel.Guild.BoostLevel}");
    }

    /// <summary>
    ///     Enable this command only on debug
    /// </summary>
    [Conditional("DEBUG")]
    private void EnableOnlyOnDebug()
    {
        Enabled = true;
    }
}
