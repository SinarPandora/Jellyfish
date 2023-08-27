using System.Diagnostics;
using Jellyfish.Core.Command;
using Jellyfish.Core.Kook;
using Kook.WebSocket;
using NLog;

namespace Jellyfish.Module;

/// <summary>
///     Simple testing command
/// </summary>
public class SimpleTestCommand : MessageCommand
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly KookApiFactory _apiFactory;

    public SimpleTestCommand(KookApiFactory apiFactory)
    {
        _apiFactory = apiFactory;
        Enabled = false;
        EnableOnlyOnDebug();
    }

    public override string Name() => "简单测试指令";

    public override string[] Keywords() => new[] { "!Test" };

    public override string Help() => throw new NotSupportedException("测试指令不包含帮助信息也不应该对用户开放");

    public override async Task<CommandResult> Execute(SocketMessage msg, SocketGuildUser user,
        SocketTextChannel channel)
    {
        if (msg.Content != "!Test" && msg.Content != "！Test") return CommandResult.Continue;

        await channel.SendTextAsync("Simple hello world!");

        Log.Info($"Current Boot Level is {channel.Guild.BoostLevel}");

        return CommandResult.Done;
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
