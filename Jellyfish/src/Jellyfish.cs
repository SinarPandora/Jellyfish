using Jellyfish.Loader;

namespace Jellyfish;

public static class JellyFish
{
    /// <summary>
    ///     The entry point of the application.
    /// </summary>
    public static async Task Main()
    {
        await KookLoader.Instance.Boot();
        await Task.Delay(Timeout.Infinite);
    }

}

