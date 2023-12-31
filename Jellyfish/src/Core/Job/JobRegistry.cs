using FluentScheduler;
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
        TeamPlayConfigCleanUpJob teamPlayConfigCleanUpJob)
    {
        Schedule(teamPlayRoomScanJob).NonReentrant().ToRunEvery(1).Minutes();
        Schedule(cacheSyncJob).NonReentrant().ToRunEvery(5).Minutes();
        Schedule(expireExtendScanJob).NonReentrant().ToRunEvery(1).Minutes();
        Schedule(cleanNonExistTmpTextChannelJob).NonReentrant().ToRunEvery(3).Minutes();
        Schedule(teamPlayConfigCleanUpJob).NonReentrant().ToRunEvery(1).Days();
    }
}
