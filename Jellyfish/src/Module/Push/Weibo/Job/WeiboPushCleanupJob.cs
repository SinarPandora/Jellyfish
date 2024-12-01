using FluentScheduler;
using Jellyfish.Core.Data;
using Kook.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Module.Push.Weibo.Job;

/// <summary>
///     Cleanup history before 1 month in midnight
///     Cleanup configs and instances where the Guild or the Channel do not exist anymore.
/// </summary>
public class WeiboPushCleanupJob(BaseSocketClient kook, DbContextProvider dbProvider, ILogger<WeiboPushCleanupJob> log)
    : IAsyncJob
{
    public async Task ExecuteAsync()
    {
        log.LogInformation("开始清理过期微博和推送配置");
        await using var dbCtx = dbProvider.Provide();

        // Clean up config where the Guild not exist
        var inactiveConfigs = dbCtx.WeiboPushConfigs.ToList().Where(config => kook.GetGuild(config.GuildId) is null)
            .ToList();
        dbCtx.WeiboPushConfigs.RemoveRange(inactiveConfigs);
        dbCtx.SaveChanges();
        log.LogInformation("已清理{Count}条过期服务器数据", inactiveConfigs.Count);

        // Clean up config where the Channel not exist
        var inactiveInstances = dbCtx.WeiboPushInstances
            .Include(i => i.Config).ToList()
            .Where(instance => kook.GetGuild(instance.Config.GuildId)?.GetTextChannel(instance.ChannelId) is null)
            .ToList();
        dbCtx.WeiboPushInstances.RemoveRange(inactiveInstances);
        dbCtx.SaveChanges();
        log.LogInformation("已清理{Count}条过期频道数据", inactiveInstances.Count);

        // Cleanup history before 1 month in midnight
        var oneMonthAgo = DateTime.Now.AddMonths(-1);
        var inactiveHistories = dbCtx.WeiboCrawlHistories.Where(h => h.CreateTime < oneMonthAgo).ToList();
        dbCtx.WeiboCrawlHistories.RemoveRange(inactiveHistories);
        dbCtx.SaveChanges();
        log.LogInformation("已清理{Count}条过期微博数据", inactiveHistories.Count);
        log.LogInformation("过期微博和推送配置清理完成");
    }
}
