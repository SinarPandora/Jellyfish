using FluentScheduler;
using Jellyfish.Module.Role.Job;

namespace Jellyfish.Core.Job;

/// <summary>
///     Schedule job registry
/// </summary>
public class JobRegistry : Registry
{
    public JobRegistry()
    {
        Schedule<RefreshRoleCacheJob>().ToRunEvery(1).Days().At(4, 0);
    }
}
