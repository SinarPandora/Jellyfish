using FluentScheduler;
using Jellyfish.Module.TeamPlay.Job;

namespace Jellyfish.Core.Job;

/// <summary>
///     Schedule job registry
/// </summary>
public class JobRegistry : Registry
{
    // ReSharper disable once SuggestBaseTypeForParameterInConstructor
    public JobRegistry(TeamPlayRoomScanJob teamPlayRoomScanJob)
    {
        Schedule(teamPlayRoomScanJob).ToRunEvery(1).Minutes();
    }
}
