using Jellyfish.Core.Command;
using Jellyfish.Module.TeamPlay.Core;
using Jellyfish.Util;
using Kook;
using Kook.WebSocket;
using NLog;

namespace Jellyfish.Module.TeamPlay;

/// <summary>
///     Team play card action
/// </summary>
public class TeamPlayButtonActionEntry : ButtonActionCommand
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public override string Name() => "组队游戏卡片操作";

    public override async Task<CommandResult> Execute(string value, Cacheable<SocketGuildUser, ulong> user,
        Cacheable<IMessage, Guid> message, SocketTextChannel channel)
    {
        if (!value.StartsWith("tp_bind_")) return CommandResult.Continue;

        var args = Regexs.MatchSingleDash().Split(value[8..], 2);
        if (!ulong.TryParse(args[0], out var userId) || userId != user.Value.Id)
        {
            Log.Info($"已阻止用户 {user.Value.Username} 操作不属于他的卡片按钮");
            return CommandResult.Done;
        }

        await TeamPlayManageService.BindingVoiceChannel(args[1], user, channel);
        return CommandResult.Done;
    }
}
