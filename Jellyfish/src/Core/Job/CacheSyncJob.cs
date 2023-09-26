using FluentScheduler;
using Kook.WebSocket;
using NLog;

namespace Jellyfish.Core.Job;

/// <summary>
///     Auto sync caches
/// </summary>
public class CacheSyncJob : IAsyncJob
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly KookSocketClient _kook;

    public CacheSyncJob(KookSocketClient kook)
    {
        _kook = kook;
    }

    public async Task ExecuteAsync()
    {
        Log.Info("开始拉取服务器缓存...");
        try
        {
            await _kook.DownloadUsersAsync();
            foreach (var guild in _kook.Guilds)
            {
                await guild.DownloadBoostSubscriptionsAsync();
            }

            Log.Info("服务器缓存拉取成功");
        }
        catch (Exception e)
        {
            Log.Error(e, "服务器缓存拉取失败");
        }
    }
}
