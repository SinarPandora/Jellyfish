using FluentScheduler;
using Kook.WebSocket;

namespace Jellyfish.Core.Job;

/// <summary>
///     Auto sync caches
/// </summary>
public class CacheSyncJob(BaseSocketClient kook, ILogger<CacheSyncJob> log) : IAsyncJob
{
    public async Task ExecuteAsync()
    {
        log.LogInformation("开始拉取服务器缓存...");
        try
        {
            await kook.DownloadUsersAsync();
            foreach (var guild in kook.Guilds)
            {
                await guild.DownloadBoostSubscriptionsAsync();
            }

            log.LogInformation("服务器缓存拉取成功");
        }
        catch (Exception e)
        {
            log.LogError(e, "服务器缓存拉取失败");
        }
    }
}
