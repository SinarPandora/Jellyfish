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
        CleanNonExistTmpTextChannelJob cleanNonExistTmpTextChannelJob)
    {
        Schedule(teamPlayRoomScanJob).ToRunEvery(1).Minutes();
        Schedule(cacheSyncJob).ToRunEvery(5).Minutes();
        Schedule(expireExtendScanJob).ToRunEvery(1).Minutes();
        Schedule(cleanNonExistTmpTextChannelJob).ToRunEvery(3).Minutes();
    }
}
