using FluentScheduler;

namespace Jellyfish.Core.Job;

/// <summary>
///     Schedule job loader
/// </summary>
public class JobLoader
{
    private readonly ILogger<JobLoader> _log;
    private readonly Registry _registry;

    public JobLoader(Registry registry, ILogger<JobLoader> log)
    {
        _registry = registry;
        _log = log;
    }

    public void Load()
    {
        _log.LogInformation("开始配置定时任务");
        JobManager.Initialize(_registry);
        _log.LogInformation("定时任务配置完成");
    }
}
