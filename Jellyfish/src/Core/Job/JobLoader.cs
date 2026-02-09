using FluentScheduler;
using Jellyfish.Module.ClockIn.Job;
using Jellyfish.Module.CountDownName.Job;
using Jellyfish.Module.ExpireExtendSession.Job;
using Jellyfish.Module.Push.Weibo.Job;
using Jellyfish.Module.RecallMessageMonitor.Job;
using Jellyfish.Module.TeamPlay.Job;
using Jellyfish.Module.TmpChannel.Job;

namespace Jellyfish.Core.Job;

/// <summary>
///     Schedule job loader
/// </summary>
public class JobLoader(
    ILogger<JobLoader> log,
    TeamPlayRoomScanJob teamPlayRoomScanJob,
    CacheSyncJob cacheSyncJob,
    ExpireExtendScanJob expireExtendScanJob,
    CleanNonExistTmpTextChannelJob cleanNonExistTmpTextChannelJob,
    TeamPlayConfigCleanUpJob teamPlayConfigCleanUpJob,
    CountDownScanJob countDownScanJob,
    ClockInMessageSyncJob clockInMessageSyncJob,
    ClockInStageScanJob clockInStageScanJob,
    ClockInStageQualifiedRoleSyncJob clockInStageQualifiedRoleSyncJob,
    WeiboScanAndPushJob weiboScanAndPushJob,
    WeiboPushCleanupJob weiboPushCleanupJob,
    WeiboPushFallbackJob weiboPushFallbackJob,
    EnsureMessageRecalledJob ensureMessageRecalledJob
)
{
    private readonly List<Schedule> _schedules = new();

    public void Load()
    {
        log.LogInformation("开始配置定时任务");
        var teamPlayRoomScan = new Schedule(
            async () => await teamPlayRoomScanJob.ExecuteAsync(),
            run => run.Every(1).Minutes()
        );
        var cacheSync = new Schedule(
            async () => await cacheSyncJob.ExecuteAsync(),
            run => run.Every(10).Minutes()
        );
        var expireExtendScan = new Schedule(
            async () => await expireExtendScanJob.ExecuteAsync(),
            run => run.Every(1).Minutes()
        );
        var cleanNonExistTmpTextChannel = new Schedule(
            async () => await cleanNonExistTmpTextChannelJob.ExecuteAsync(),
            run => run.Every(3).Minutes()
        );
        var teamPlayConfigCleanUp = new Schedule(
            async () => await teamPlayConfigCleanUpJob.ExecuteAsync(),
            run => run.Every(1).Days().At(1, 0)
        );
        var countDownScan = new Schedule(
            async () => await countDownScanJob.ExecuteAsync(),
            run => run.Every(1).Days().At(0, 0)
        );
        var clockInMessageSync = new Schedule(
            async () => await clockInMessageSyncJob.ExecuteAsync(),
            run => run.Every(1).Minutes()
        );
        var clockInStageScan = new Schedule(
            async () => await clockInStageScanJob.ExecuteAsync(),
            run => run.Every(1).Minutes()
        );
        var clockInStageQualifiedRoleSync = new Schedule(
            async () => await clockInStageQualifiedRoleSyncJob.ExecuteAsync(),
            run => run.Every(5).Minutes()
        );
        var weiboScanAndPush = new Schedule(
            async () => await weiboScanAndPushJob.ExecuteAsync(),
            run => run.Every(2).Minutes()
        );
        var weiboPushFallback = new Schedule(
            async () => await weiboPushFallbackJob.ExecuteAsync(),
            run => run.Every(10).Minutes()
        );
        var weiboPushCleanup = new Schedule(
            async () => await weiboPushCleanupJob.ExecuteAsync(),
            run => run.Every(1).Days().At(2, 0)
        );
        var ensureMessageRecalled = new Schedule(
            async () => await ensureMessageRecalledJob.ExecuteAsync(),
            run => run.Every(3).Minutes()
        );
        this._schedules.AddRange([
            teamPlayRoomScan,
            cacheSync,
            expireExtendScan,
            cleanNonExistTmpTextChannel,
            teamPlayConfigCleanUp,
            countDownScan,
            clockInMessageSync,
            clockInStageScan,
            clockInStageQualifiedRoleSync,
            weiboScanAndPush,
            weiboPushFallback,
            weiboPushCleanup,
            ensureMessageRecalled,
        ]);
        log.LogInformation("定时任务配置完成");
    }

    public void Start()
    {
        this._schedules.Start();
    }

    public void Stop()
    {
        this._schedules.StopAndBlock();
    }
}
