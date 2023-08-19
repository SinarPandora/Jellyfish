using Ninject;

namespace Jellyfish.Loader;

public static class AppContext
{
    public static readonly StandardKernel Instance = new(new AppModule());
}
