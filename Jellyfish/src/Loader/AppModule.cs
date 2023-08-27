using Jellyfish.Config;
using Jellyfish.Core.Command;
using Jellyfish.Module;
using Jellyfish.Module.TeamPlay;
using Kook.WebSocket;
using Ninject;
using Ninject.Modules;

namespace Jellyfish.Loader;

/// <summary>
///     Contains all singleton instances in the application
/// </summary>
public class AppModule : NinjectModule
{
    /// <summary>
    ///     Load application context
    /// </summary>
    public override void Load()
    {
        // ------------------------------------------------ System -----------------------------------------------------
        Bind<AppConfig>().To<AppConfig>().InSingletonScope();
        Bind<KookSocketClient>()
            .ToMethod(ctx => KookLoader.CreateSocketClient(ctx.Kernel.Get<AppConfig>()))
            .InSingletonScope();
        Bind<KookLoader>().To<KookLoader>().InSingletonScope();
        Bind<KookApiFactory>().To<KookApiFactory>().InSingletonScope();
        Bind<EventMatcher>().To<EventMatcher>().InSingletonScope();

        // ------------------------------------------------ Commands ---------------------------------------------------
        // Simple Command
        Bind<IMessageCommand>().To<SimpleTestCommand>().InSingletonScope();

        // TeamPlay Command
        Bind<IMessageCommand>().To<TeamPlayEntryCommand>().InSingletonScope();
        Bind<TeamPlayUserAction>().To<TeamPlayUserAction>().InSingletonScope();
        Bind<IButtonActionCommand>().To<TeamPlayButtonActionEntry>().InSingletonScope();
    }
}
