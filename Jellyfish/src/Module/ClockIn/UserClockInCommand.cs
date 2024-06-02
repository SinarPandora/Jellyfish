using Jellyfish.Core.Command;
using Jellyfish.Module.ClockIn.Core;
using Kook.WebSocket;

namespace Jellyfish.Module.ClockIn;

/// <summary>
///     Command for user clock-in
/// </summary>
public class UserClockInCommand(ClockInBuffer buffer) : GuildMessageCommand(false)
{
    public override string Name() => "用户打卡指令";

    public override IEnumerable<string> Keywords() => ["/打卡"];

    protected override Task Execute(string args, string keyword, SocketMessage msg, SocketGuildUser user,
        SocketTextChannel channel)
    {
        buffer.Instance.OnNext((channel.Guild.Id, channel.Id, user.Id));
        return Task.CompletedTask;
    }
}
