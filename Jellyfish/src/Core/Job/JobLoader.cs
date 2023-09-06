using FluentScheduler;
using NLog;

namespace Jellyfish.Core.Job;

/// <summary>
///     Schedule job loader
/// </summary>
public class JobLoader
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly Registry _registry;

    public JobLoader(Registry registry)
    {
        _registry = registry;
    }

    public void Load()
    {
        Log.Info("开始配置定时任务");
        JobManager.Initialize(_registry);
        Log.Info("定时任务配置完成");
    }
}
