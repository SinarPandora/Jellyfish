using Ninject;

namespace Jellyfish.Loader;

/// <summary>
///     The instance of application context
/// </summary>
public static class AppContext
{
    public static readonly StandardKernel Instance = new(new AppModule());
}
