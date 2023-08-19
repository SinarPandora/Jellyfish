using Jellyfish.Command;
using Jellyfish.Command.TeamPlay;
using Jellyfish.Config;
using Jellyfish.Core;
using Kook.WebSocket;
using Ninject.Modules;

namespace Jellyfish.Loader;

public class AppModule : NinjectModule
{
    public override void Load()
    {
        Bind<AppConfig>().To<AppConfig>().InSingletonScope();
        Bind<KookSocketClient>().ToConstant(new KookSocketClient(new KookSocketConfig
        {
            AlwaysDownloadUsers = true,
            AlwaysDownloadVoiceStates = true
        })).InSingletonScope();
        Bind<KookLoader>().To<KookLoader>().InSingletonScope();
        Bind<KookApiFactory>().To<KookApiFactory>().InSingletonScope();
        Bind<EventMatcher>().To<EventMatcher>().InSingletonScope();
        Bind<SimpleHelloCommand>().To<SimpleHelloCommand>().InSingletonScope();
        Bind<TeamPlayUserAction>().To<TeamPlayUserAction>().InSingletonScope();
        Bind<TeamPlayManagerAction>().To<TeamPlayManagerAction>().InSingletonScope();
        Bind<TeamPlayEntryCommand>().To<TeamPlayEntryCommand>().InSingletonScope();
    }
}
