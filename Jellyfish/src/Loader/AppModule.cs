using Jellyfish.Command;
using Jellyfish.Command.TeamPlay;
using Jellyfish.Config;
using Jellyfish.Core.Command;
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
        Bind<IMessageCommand>().To<SimpleHelloCommand>().InSingletonScope();

        // TeamPlay Command
        Bind<IMessageCommand>().To<TeamPlayEntryCommand>().InSingletonScope();
        Bind<TeamPlayUserAction>().To<TeamPlayUserAction>().InSingletonScope();
        Bind<TeamPlayManagerAction>().To<TeamPlayManagerAction>().InSingletonScope();
        Bind<IButtonActionCommand>().To<TeamPlayButtonActionEntryCommand>().InSingletonScope();
    }
}
