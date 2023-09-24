using FluentScheduler;
using Jellyfish.Core.Command;
using Jellyfish.Core.Config;
using Jellyfish.Core.Job;
using Jellyfish.Core.Kook;
using Jellyfish.Module;
using Jellyfish.Module.Help;
using Jellyfish.Module.Role;
using Jellyfish.Module.TeamPlay;
using Jellyfish.Module.TeamPlay.Core;
using Jellyfish.Module.TeamPlay.Job;
using Jellyfish.Module.TeamPlay.Manage;
using Jellyfish.Module.TeamPlay.User;
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
        Bind<Registry>().To<JobRegistry>().InSingletonScope();
        Bind<JobLoader>().To<JobLoader>().InSingletonScope();

        // ------------------------------------------------ Commands ---------------------------------------------------
        // Simple Command
        Bind<GuildMessageCommand>().To<SimpleTestCommand>().InSingletonScope();

        // TeamPlay Command
        Bind<GuildMessageCommand>().To<TeamPlayManageCommand>().InSingletonScope();
        Bind<GuildMessageCommand>().To<TeamPlayUserCommand>().InSingletonScope();
        Bind<DmcCommand>().To<TeamPlayRoomUpdateDmcCommand>().InSingletonScope();
        Bind<ButtonActionCommand>().To<TeamPlayButtonActionEntry>().InSingletonScope();
        Bind<TeamPlayRoomScanJob>().To<TeamPlayRoomScanJob>().InSingletonScope();
        Bind<TeamPlayRoomService>().To<TeamPlayRoomService>().InSingletonScope();
        Bind<UserConnectEventCommand>().To<TeamPlayClickToJoinCommand>().InSingletonScope();

        // Role Command
        Bind<GuildMessageCommand>().To<RoleSettingCommand>().InSingletonScope();

        // Help Command
        Bind<GuildMessageCommand>().To<GlobalHelpCommand>().InSingletonScope();
    }
}
