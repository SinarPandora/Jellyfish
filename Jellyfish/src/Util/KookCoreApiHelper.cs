using Kook.WebSocket;
using NLog;
using Polly;

namespace Jellyfish.Util;

/// <summary>
///     The core Kook API in the system, for minimizes errors, retry mechanism is configured
/// </summary>
public static class KookCoreApiHelper
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    ///     Delete channel, core API, retry in 2 times if error occur
    /// </summary>
    /// <param name="guild"></param>
    /// <param name="voiceChannelId"></param>
    /// <returns></returns>
    public static Task DeleteVoiceChannelAsync(this SocketGuild guild, ulong voiceChannelId)
    {
        var voiceChannel = guild.GetVoiceChannel(voiceChannelId);
        return Policy
            .Handle<Exception>()
            .Retry(2, (e, i) =>
            {
                Log.Warn(e, $"删除频道 API 调用失败一次，房间名：{voiceChannel.Name}，重试次数：{i}");
                voiceChannel = guild.GetVoiceChannel(voiceChannelId);
            })
            .Execute(async () =>
            {
                if (voiceChannel != null)
                {
                    await voiceChannel.DeleteAsync();
                }
            });
    }
}
