using FluentScheduler;
using Jellyfish.Module.ClockIn.Job;
using Jellyfish.Module.CountDownName.Job;
using Jellyfish.Module.ExpireExtendSession.Job;
using Jellyfish.Module.TeamPlay.Job;
using Jellyfish.Module.TmpChannel.Job;

namespace Jellyfish.Core.Job;

/// <summary>
///     Schedule job registry
/// </summary>
public class JobRegistry : Registry
{
    // ReSharper disable SuggestBaseTypeForParameterInConstructor
    public JobRegistry(
        TeamPlayRoomScanJob teamPlayRoomScanJob,
        CacheSyncJob cacheSyncJob,
        ExpireExtendScanJob expireExtendScanJob,
        CleanNonExistTmpTextChannelJob cleanNonExistTmpTextChannelJob,
        TeamPlayConfigCleanUpJob teamPlayConfigCleanUpJob,
        CountDownScanJob countDownScanJob,
        ClockInMessageSyncJob clockInMessageSyncJob,
        ClockInStageScanJob clockInStageScanJob,
        ClockInStageQualifiedRoleSyncJob clockInStageQualifiedRoleSyncJob)
    {
        Schedule(teamPlayRoomScanJob).NonReentrant().ToRunEvery(1).Minutes();
        Schedule(cacheSyncJob).NonReentrant().ToRunEvery(10).Minutes();
        Schedule(expireExtendScanJob).NonReentrant().ToRunEvery(1).Minutes();
        Schedule(cleanNonExistTmpTextChannelJob).NonReentrant().ToRunEvery(3).Minutes();
        Schedule(teamPlayConfigCleanUpJob).NonReentrant().ToRunEvery(1).Days().At(1, 0);
        Schedule(countDownScanJob).NonReentrant().ToRunEvery(1).Days().At(0, 0);
        Schedule(clockInMessageSyncJob).NonReentrant().ToRunEvery(1).Minutes();
        Schedule(clockInStageScanJob).NonReentrant().ToRunEvery(1).Minutes();
        Schedule(clockInStageQualifiedRoleSyncJob).NonReentrant().ToRunEvery(5).Minutes();
    }
}
