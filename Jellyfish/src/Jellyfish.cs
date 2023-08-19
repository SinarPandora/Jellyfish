using Jellyfish.Loader;
using Ninject;
using AppContext = Jellyfish.Loader.AppContext;

namespace Jellyfish;

public static class JellyFish
{
    /// <summary>
    ///     The entry point of the application.
    /// </summary>
    public static async Task Main()
    {
        await AppContext.Instance.Get<KookLoader>().Boot();
        await Task.Delay(Timeout.Infinite);
    }
}
