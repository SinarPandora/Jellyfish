using Jellyfish.Core.Cache;
using Jellyfish.Core.Command;
using Jellyfish.Module.TeamPlay.Core;
using Jellyfish.Util;
using Kook;
using Kook.WebSocket;

namespace Jellyfish.Module.TeamPlay.User;

/// <summary>
///     Create room command, trigger when user click and join to any specified voice channel
/// </summary>
public class TeamPlayClickToJoinCommand : UserConnectEventCommand
{
    private readonly TeamPlayRoomService _service;
    private readonly KookSocketClient _kook;

    public TeamPlayClickToJoinCommand(TeamPlayRoomService service, KookSocketClient kook)
    {
        _service = service;
        _kook = kook;
    }

    public override string Name() => "语音频道点击创建房间指令";

    public override async Task<CommandResult> Execute(Cacheable<SocketGuildUser, ulong> user,
        SocketVoiceChannel channel, DateTimeOffset joinAt)
    {
        var tpConfig = (from config in AppCaches.TeamPlayConfigs.Values
            where config.GuildId == channel.Guild.Id && config.VoiceChannelId == channel.Id
            select config).FirstOrDefault();

        if (tpConfig == null) return CommandResult.Continue;
        await _service.CreateAndMoveToRoom(CreateRoomCommandParser.Parse(string.Empty)(tpConfig), user.Value,
            async (_, room) =>
            {
                if (tpConfig.TextChannelId != null)
                {
                    await _kook
                        .GetGuild(tpConfig.GuildId)
                        .GetTextChannel((ulong)tpConfig.TextChannelId)
                        .SendCardAsync(await TeamPlayRoomService.CreateInviteCard(room));
                }

                // Show room update hits in private channel
                var dmc = await user.Value.CreateDMChannelAsync();
                await dmc.SendSuccessCardAsync(
                    $"""
                     您已创建房间{room.Name}
                     ---
                     您可以发送以下指令修改房间信息：
                     ```
                     - /改名 [新房间名]
                     - /密码 [房间密码，1~12 位纯数字]
                     - /人数 [设置房间人数，1 以上整数，或 “无限制”]
                     ```
                     ---
                     当所有人退出房间后，房间将被解散。
                     您也可以发送：`/解散` 来立刻解散当前房间。
                     """);
            });
        return CommandResult.Done;
    }
}
