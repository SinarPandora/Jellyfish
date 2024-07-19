using Jellyfish.Core.Command;
using Jellyfish.Module.ClockIn.Core;
using Kook;
using Kook.WebSocket;

namespace Jellyfish.Module.ClockIn;

/// <summary>
///     Button actions for clock-in card
/// </summary>
public class ClockInCardAction(ClockInBuffer buffer) : ButtonActionCommand
{
    public const string CardActionValue = "$clock_in";

    public override string Name() => "用户卡片打卡操作";

    public override Task<CommandResult> Execute(string value, Cacheable<SocketGuildUser, ulong> user,
        Cacheable<IMessage, Guid> message, SocketTextChannel channel)
    {
        if (!value.StartsWith(CardActionValue)) return Task.FromResult(CommandResult.Continue);
        buffer.Instance.OnNext((channel.Guild.Id, channel.Id, user.Id, true));
        return Task.FromResult(CommandResult.Done);
    }
}
