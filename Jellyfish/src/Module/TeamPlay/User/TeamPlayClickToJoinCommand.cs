using Jellyfish.Core.Cache;
using Jellyfish.Core.Command;
using Jellyfish.Module.TeamPlay.Core;
using Kook;
using Kook.WebSocket;

namespace Jellyfish.Module.TeamPlay.User;

/// <summary>
///     Create room command, trigger when user click and join to any specified voice channel
/// </summary>
public class TeamPlayClickToJoinCommand : UserConnectEventCommand
{
    private readonly TeamPlayRoomService _service;

    public TeamPlayClickToJoinCommand(TeamPlayRoomService service)
    {
        _service = service;
    }

    public override string Name() => "语音频道点击创建房间指令";

    public override async Task<CommandResult> Execute(Cacheable<SocketGuildUser, ulong> user,
        SocketVoiceChannel channel, DateTimeOffset joinAt)
    {
        var tpConfig = (from config in AppCaches.TeamPlayConfigs.Values
            where config.GuildId == channel.Guild.Id && config.VoiceChannelId == channel.Id
            select config).FirstOrDefault();

        if (tpConfig == null) return CommandResult.Continue;
        await _service.CreateAndMoveToRoomAsync(CreateRoomCommandParser.Parse(string.Empty)(tpConfig), user.Value, null,
            async (_, room) =>
            {
                var notifyChannelId = tpConfig.CreationNotifyChannelId ?? tpConfig.TextCategoryId;
                if (notifyChannelId.HasValue)
                {
                    await channel.Guild
                        .GetTextChannel(notifyChannelId.Value)
                        .SendCardAsync(await TeamPlayRoomService.CreateInviteCardAsync(room));
                }
            });
        return CommandResult.Done;
    }
}
