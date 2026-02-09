using Jellyfish.Core.Cache;
using Jellyfish.Core.Command;
using JetBrains.Annotations;
using Kook.WebSocket;

namespace Jellyfish.Module.GuildSetting.Core;

/// <summary>
///     Resolve conflict where other bots reported an error
///     after failing to recognize the Jellyfish Bot command
///     (directly withdrawing the specified message)
/// </summary>
public class SynergyBotConflictResolveCommand() : GuildMessageCommand(false)
{
    public override string Name() => "协同机器人冲突解决指令";

    public override IEnumerable<string> Keywords() => [];

    [UsedImplicitly]
    public new async Task<CommandResult> MatchAndExecute(
        SocketMessage msg,
        SocketGuildUser user,
        SocketTextChannel channel
    )
    {
        var bots = AppCaches.GuildSettings[channel.Guild.Id].SynergyBotAccounts;
        var messages = AppCaches.GuildSettings[channel.Guild.Id].SynergyBotConflictMessage;
        if (
            !(user.IsBot ?? false)
            || !bots.Contains(user.Id)
            || messages.IsEmpty()
            || !messages.Any(m => msg.Content.Contains(m))
        )
        {
            return CommandResult.Continue;
        }

        await msg.DeleteAsync();
        return CommandResult.Done;
    }

    protected override Task Execute(
        string args,
        string keyword,
        SocketMessage msg,
        SocketGuildUser user,
        SocketTextChannel channel
    )
    {
        throw new NotSupportedException("Please use MatchAndExecute method instead");
    }
}
