using Kook;
using Kook.WebSocket;
using NLog;
using NLog.Web;
using Polly;

namespace Jellyfish.Util;

/// <summary>
///     The core Kook API in the system, for minimizes errors, retry mechanism is configured
/// </summary>
public static class KookCoreApiHelper
{
    private static readonly Logger Log = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

    /// <summary>
    ///     Delete channel, core API, retry in 2 times if error occur
    /// </summary>
    /// <param name="guild">Current guild</param>
    /// <param name="voiceChannelId">Voice channel id</param>
    /// <returns></returns>
    public static Task DeleteVoiceChannelAsync(this SocketGuild guild, ulong voiceChannelId)
    {
        var voiceChannel = guild.GetVoiceChannel(voiceChannelId);
        return Policy
            .Handle<Exception>()
            .RetryAsync(2, (e, i) =>
            {
                Log.Warn(e, $"删除频道 API 调用失败一次，房间名：{voiceChannel.Name}，重试次数：{i}");
                voiceChannel = guild.GetVoiceChannel(voiceChannelId);
            })
            .ExecuteAsync(async () =>
            {
                if (voiceChannel != null)
                {
                    await voiceChannel.DeleteAsync();
                }
            });
    }

    /// <summary>
    ///     Move user to room, core API, retry in 2 times if error occur
    /// </summary>
    /// <param name="guild">Current guild</param>
    /// <param name="user">Target user</param>
    /// <param name="toChannel">To channel</param>
    public static async Task MoveToRoomAsync(this IGuild guild, SocketGuildUser user, IVoiceChannel toChannel)
    {
        var currentChannel = (await user.GetConnectedVoiceChannelsAsync()).FirstOrDefault();
        var guildUsers = new List<IGuildUser> { user };
        await Policy
            .Handle<Exception>()
            .RetryAsync(2,
                async (e, i) =>
                {
                    Log.Warn(e, $"移动频道 API 调用失败一次，用户名：{user.DisplayName()}，房间名：{toChannel.Name}，重试次数：{i}");
                    currentChannel = (await user.GetConnectedVoiceChannelsAsync()).FirstOrDefault();
                })
            .ExecuteAsync(async () =>
            {
                if (currentChannel == null) return;
                if (currentChannel.Id != toChannel.Id)
                {
                    await guild.MoveUsersAsync(guildUsers, toChannel);
                }
            });
    }
}
