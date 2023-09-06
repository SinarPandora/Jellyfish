using Jellyfish.Core.Cache;
using Jellyfish.Core.Job;
using Jellyfish.Core.Kook;
using Ninject;
using AppContext = Jellyfish.Core.Container.AppContext;

namespace Jellyfish;

public static class JellyFish
{
    /// <summary>
    ///     The entry point of the application.
    /// </summary>
    public static async Task Main()
    {
        await CacheLoader.Load();
        await AppContext.Instance.Get<KookLoader>().Load();
        AppContext.Instance.Get<JobLoader>().Load();
        await Task.Delay(Timeout.Infinite);
    }
}
