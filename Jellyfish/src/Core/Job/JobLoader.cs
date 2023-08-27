using FluentScheduler;
using NLog;

namespace Jellyfish.Core.Job;

/// <summary>
///     Schedule job loader
/// </summary>
public abstract class JobLoader
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static void Load()
    {
        Log.Info("开始配置定时任务");
        JobManager.Initialize(new JobRegistry());
        Log.Info("定时任务配置完成");
    }
}
