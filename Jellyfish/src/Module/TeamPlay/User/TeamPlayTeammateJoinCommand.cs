using Jellyfish.Core.Command;
using Jellyfish.Core.Data;
using Jellyfish.Util;
using Kook;
using Kook.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Module.TeamPlay.User;

/// <summary>
///     Teammate join to team play room command
///     Grant permission for teammate
/// </summary>
public class TeamPlayTeammateJoinCommand : UserConnectEventCommand
{
    private readonly ILogger<TeamPlayTeammateJoinCommand> _log;
    private readonly DatabaseContext _dbCtx;
    private readonly KookSocketClient _kook;

    public TeamPlayTeammateJoinCommand(DatabaseContext dbCtx, ILogger<TeamPlayTeammateJoinCommand> log,
        KookSocketClient kook)
    {
        _dbCtx = dbCtx;
        _log = log;
        _kook = kook;
    }

    public override string Name() => "队友加入组队语音频道指令";

    public override async Task<CommandResult> Execute(Cacheable<SocketGuildUser, ulong> user,
        SocketVoiceChannel channel,
        DateTimeOffset joinAt)
    {
        var room = _dbCtx.TpRoomInstances.Include(e => e.TmpTextChannel)
            .FirstOrDefault(room => room.VoiceChannelId == channel.Id && room.GuildId == channel.Guild.Id);

        if (room?.TmpTextChannel == null) return CommandResult.Continue;

        room.UpdateTime = DateTime.Now; // Any user enter will refresh the expire time
        var tmpInstance = room.TmpTextChannel;
        var restGuild = await _kook.Rest.GetGuildAsync(channel.Guild.Id);
        var restTextChannel = await restGuild.GetTextChannelAsync(tmpInstance.ChannelId);
        if (restTextChannel == null)
        {
            room.TmpTextChannelId = null;
            _dbCtx.SaveChanges();
            _log.LogInformation("检测到文字房间已被删除，已删除组队房间中临时文字房间的绑定关系");
            _log.LogInformation("数据库中的临时文字房间实例将在稍后由扫描任务清理");
        }
        else if (restTextChannel.GetPermissionOverwrite(user.Value) == null)
        {
            await restTextChannel.OverrideUserPermissionAsync(user.Value, p => p.Modify(
                viewChannel: PermValue.Allow,
                mentionEveryone: PermValue.Allow
            ));
            _log.LogInformation("加入组队房间 {TpRoomName} 的用户：{Name}:{Id}，已获得文字房间 {TextRoomName} 的访问权限",
                room.RoomName, user.Value.DisplayName(), user.Id, restTextChannel.Name
            );
            await restTextChannel.SendSuccessCardAsync($"欢迎 {MentionUtils.KMarkdownMentionUser(user.Id)} 加入组队房间！",
                false);
        }

        return CommandResult.Done;
    }
}
