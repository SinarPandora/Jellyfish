using FluentScheduler;

namespace Jellyfish.Core.Job;

/// <summary>
///     Schedule job loader
/// </summary>
public class JobLoader(Registry registry, ILogger<JobLoader> log)
{
    public void Load()
    {
        log.LogInformation("开始配置定时任务");
        JobManager.Initialize(registry);
        log.LogInformation("定时任务配置完成");
    }
}
