using Jellyfish.Core.Cache;
using Jellyfish.Core.Command;
using Jellyfish.Module.TeamPlay.Core;
using Jellyfish.Util;
using Kook;
using Kook.WebSocket;

namespace Jellyfish.Module.TeamPlay.User;

/// <summary>
///     Create room command, trigger when user clicks and joins to any specified voice channel
/// </summary>
public class TeamPlayClickToJoinCommand(TeamPlayRoomService service) : UserConnectEventCommand
{
    public override string Name() => "语音频道点击创建房间指令";

    public override async Task<CommandResult> Execute(Cacheable<SocketGuildUser, ulong> user,
        SocketVoiceChannel channel, DateTimeOffset joinAt)
    {
        var tpConfig = (from config in AppCaches.TeamPlayConfigs.Values
            where config.GuildId == channel.Guild.Id && config.VoiceChannelId == channel.Id
            select config).FirstOrDefault();

        if (tpConfig is null) return CommandResult.Continue;
        await service.CreateAndMoveToRoomAsync(CreateRoomCommandParser.Parse(string.Empty)(tpConfig), user.Value!, null,
            async (_, voiceChannel, textChannel) =>
            {
                var notifyChannelId = tpConfig.CreationNotifyChannelId ?? tpConfig.TextChannelId;
                if (notifyChannelId.HasValue)
                {
                    var notifyChannel = channel.Guild.GetTextChannel(notifyChannelId.Value);
                    if (notifyChannel is not null)
                    {
                        await notifyChannel.SendCardSafeAsync(
                            await TeamPlayRoomService.CreateInviteCardAsync(voiceChannel));
                        await notifyChannel.SendTextSafeAsync(
                            $"👍🏻想一起玩？点击上方按钮加入语音房间！{
                                (!voiceChannel.HasPassword && textChannel is not null
                                    ? $"不方便语音也可以加入同名文字房间 {MentionUtils.KMarkdownMentionChannel(textChannel.Id)} 哦"
                                    : string.Empty
                                )
                            }");
                    }
                }
            });
        return CommandResult.Done;
    }
}
