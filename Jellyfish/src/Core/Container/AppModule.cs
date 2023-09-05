using Jellyfish.Core.Command;
using Jellyfish.Core.Config;
using Jellyfish.Core.Kook;
using Jellyfish.Module;
using Jellyfish.Module.Help;
using Jellyfish.Module.Role;
using Jellyfish.Module.TeamPlay;
using Jellyfish.Module.TeamPlay.Core;
using Jellyfish.Module.TeamPlay.Manage;
using Kook.WebSocket;
using Ninject;
using Ninject.Modules;

namespace Jellyfish.Core.Container;

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
        Bind<MessageCommand>().To<SimpleTestCommand>().InSingletonScope();

        // TeamPlay Command
        Bind<MessageCommand>().To<TeamPlayManageCommand>().InSingletonScope();
        Bind<MessageCommand>().To<TeamPlayUserCommand>().InSingletonScope();
        Bind<ButtonActionCommand>().To<TeamPlayButtonActionEntry>().InSingletonScope();
        Bind<TeamPlayRoomService>().To<TeamPlayRoomService>().InSingletonScope();

        // Role Command
        Bind<MessageCommand>().To<RoleSettingCommand>().InSingletonScope();

        // Help Command
        Bind<MessageCommand>().To<GlobalHelpCommand>().InSingletonScope();
    }
}
