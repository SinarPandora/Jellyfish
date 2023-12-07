using FluentScheduler;
using Jellyfish.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Module.TeamPlay.Job;

/// <summary>
///     Cleanup outdated team play config
/// </summary>
public class TeamPlayConfigCleanUpJob(DbContextProvider dbProvider, ILogger<TeamPlayConfigCleanUpJob> log)
    : IAsyncJob
{
    /// <summary>
    ///     Entrypoint of the job
    /// </summary>
    public async Task ExecuteAsync()
    {
        log.LogInformation("过期组队配置清理任务开始");
        try
        {
            var dbCtx = dbProvider.Provide();
            var outdatedConfigs = (from config in dbCtx.TpConfigs.Include(c => c.RoomInstances)
                    where !config.Enabled && config.RoomInstances.IsNotEmpty()
                    select config)
                .ToArray();

            foreach (var outdatedConfig in outdatedConfigs)
            {
                dbCtx.TpConfigs.Remove(outdatedConfig);
            }

            await dbCtx.SaveChangesAsync();
            log.LogInformation("过期组队配置清理任务结束");
        }
        catch (Exception e)
        {
            log.LogError(e, "过期组队配置清理任务失败");
        }
    }
}
