using FluentScheduler;
using Kook.WebSocket;

namespace Jellyfish.Core.Job;

/// <summary>
///     Auto sync caches
/// </summary>
public class CacheSyncJob : IAsyncJob
{
    private readonly ILogger<CacheSyncJob> _log;
    private readonly KookSocketClient _kook;

    public CacheSyncJob(KookSocketClient kook, ILogger<CacheSyncJob> log)
    {
        _kook = kook;
        _log = log;
    }

    public async Task ExecuteAsync()
    {
        _log.LogInformation("开始拉取服务器缓存...");
        try
        {
            await _kook.DownloadUsersAsync();
            foreach (var guild in _kook.Guilds)
            {
                await guild.DownloadBoostSubscriptionsAsync();
            }

            _log.LogInformation("服务器缓存拉取成功");
        }
        catch (Exception e)
        {
            _log.LogError(e, "服务器缓存拉取失败");
        }
    }
}
