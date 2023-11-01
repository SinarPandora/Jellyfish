using FluentScheduler;
using Jellyfish.Module.ExpireExtendSession.Job;
using Jellyfish.Module.TeamPlay.Job;

namespace Jellyfish.Core.Job;

/// <summary>
///     Schedule job registry
/// </summary>
public class JobRegistry : Registry
{
    // ReSharper disable once SuggestBaseTypeForParameterInConstructor
    public JobRegistry(
        TeamPlayRoomScanJob teamPlayRoomScanJob,
        CacheSyncJob cacheSyncJob,
        ExpireExtendScanJob expireExtendScanJob)
    {
        Schedule(teamPlayRoomScanJob).ToRunEvery(1).Minutes();
        Schedule(cacheSyncJob).ToRunEvery(5).Minutes();
        Schedule(expireExtendScanJob).ToRunEvery(1).Minutes();
    }
}
