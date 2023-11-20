using FluentScheduler;
using Jellyfish.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Module.TeamPlay.Job;

/// <summary>
///     Cleanup outdated team play config
/// </summary>
public class TeamPlayConfigCleanUpJob : IAsyncJob
{
    private readonly DbContextProvider _dbProvider;
    private readonly ILogger<TeamPlayConfigCleanUpJob> _log;

    public TeamPlayConfigCleanUpJob(DbContextProvider dbProvider, ILogger<TeamPlayConfigCleanUpJob> log)
    {
        _dbProvider = dbProvider;
        _log = log;
    }

    /// <summary>
    ///     Entrypoint of the job
    /// </summary>
    public async Task ExecuteAsync()
    {
        _log.LogInformation("过期组队配置清理任务开始");
        try
        {
            var dbCtx = _dbProvider.Provide();
            var outdatedConfigs = (from config in dbCtx.TpConfigs.Include(c => c.RoomInstances)
                    where !config.Enabled && config.RoomInstances.IsNotEmpty()
                    select config)
                .ToArray();

            foreach (var outdatedConfig in outdatedConfigs)
            {
                dbCtx.TpConfigs.Remove(outdatedConfig);
            }

            await dbCtx.SaveChangesAsync();
            _log.LogInformation("过期组队配置清理任务结束");
        }
        catch (Exception e)
        {
            _log.LogError(e, "过期组队配置清理任务失败");
        }
    }
}
