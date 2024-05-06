using System.Diagnostics;
using Jellyfish.Core.Command;
using Jellyfish.Util;
using Kook.WebSocket;

namespace Jellyfish.Module;

/// <summary>
///     Simple testing command
/// </summary>
public class SimpleTestCommand : GuildMessageCommand
{
    private readonly ILogger<SimpleTestCommand> _log;

    public SimpleTestCommand(ILogger<SimpleTestCommand> log) : base(false)
    {
        _log = log;
        Enabled = false;
        EnableOnlyOnDebug();
    }

    public override string Name() => "简单测试指令";

    public override string[] Keywords() => new[] { "!test", "！test" };

    protected override async Task Execute(string args, string keyword, SocketMessage msg, SocketGuildUser user,
        SocketTextChannel channel)
    {
        await channel.SendTextSafeAsync("I'm Here!");
        _log.LogInformation("Current Boot Level is {GuildBoostLevel}", channel.Guild.BoostLevel);
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
