using Autofac;
using FluentScheduler;
using Jellyfish.Core.Cache;
using Jellyfish.Core.Command;
using Jellyfish.Core.Config;
using Jellyfish.Core.Job;
using Jellyfish.Core.Kook;
using Jellyfish.Core.Lifecycle;
using Jellyfish.Core.Puppeteer;
using Jellyfish.Custom.Splatoon.Module.SuiteSearch;
using Jellyfish.Module;
using Jellyfish.Module.ClockIn;
using Jellyfish.Module.ClockIn.Core;
using Jellyfish.Module.ClockIn.Job;
using Jellyfish.Module.CountDownName;
using Jellyfish.Module.CountDownName.Core;
using Jellyfish.Module.CountDownName.Job;
using Jellyfish.Module.ExpireExtendSession.Job;
using Jellyfish.Module.GroupControl;
using Jellyfish.Module.GuildSetting.Core;
using Jellyfish.Module.Help;
using Jellyfish.Module.Role;
using Jellyfish.Module.TeamPlay;
using Jellyfish.Module.TeamPlay.Core;
using Jellyfish.Module.TeamPlay.Job;
using Jellyfish.Module.TeamPlay.Manage;
using Jellyfish.Module.TeamPlay.User;
using Jellyfish.Module.TmpChannel.Core;
using Jellyfish.Module.TmpChannel.Job;
using Jellyfish.Util;
using Kook.WebSocket;

namespace Jellyfish.Core.Container;

/// <summary>
///     The application context, binding all instances which application needs
/// </summary>
public static class AppContext
{
    public static void BindAll(ContainerBuilder container)
    {
        // ------------------------------------------------ System -----------------------------------------------------
        container.RegisterType<CacheLoader>().SingleInstance();
        container.RegisterType<AppConfig>().SingleInstance();
        container.Register<KookSocketClient>(provider =>
            {
                var kookSocketClient = KookLoader.CreateSocketClient(provider.Resolve<AppConfig>());
                KookCoreApiHelper.Kook = kookSocketClient;
                return kookSocketClient;
            })
            .As<KookSocketClient>().As<BaseSocketClient>()
            .SingleInstance();
        container.RegisterType<KookLoader>().SingleInstance();
        container.RegisterType<KookApiFactory>().SingleInstance();
        container.RegisterType<KookEventMatcher>().SingleInstance();
        container.RegisterType<CacheSyncJob>().SingleInstance();
        container.RegisterType<BrowserPageFactory>().SingleInstance();
        container.RegisterType<JobRegistry>().As<Registry>().SingleInstance();
        container.RegisterType<JobLoader>().SingleInstance();
        container.RegisterType<AppInitializer>().As<IStartupFilter>().SingleInstance();

        // ------------------------------------------------ Kook Extension ---------------------------------------------
        container.RegisterType<TmpTextChannelService>().SingleInstance();
        container.RegisterType<CleanNonExistTmpTextChannelJob>().SingleInstance();
        container.RegisterType<ExpireExtendScanJob>().SingleInstance();

        // ------------------------------------------------ Commands ---------------------------------------------------
        // Simple Command
        container.RegisterType<SimpleTestCommand>().As<GuildMessageCommand>().SingleInstance();

        // TeamPlay Command
        container.RegisterType<TeamPlayRoomScanJob>().SingleInstance();
        container.RegisterType<TeamPlayConfigCleanUpJob>().SingleInstance();
        container.RegisterType<TeamPlayRoomService>().SingleInstance();
        container.RegisterType<TeamPlayManageService>().SingleInstance();
        container.RegisterType<TeamPlayManageCommand>().As<GuildMessageCommand>().SingleInstance();
        container.RegisterType<TeamPlayUserCommand>().As<GuildMessageCommand>().SingleInstance();
        container.RegisterType<TeamPlayButtonActionEntry>().As<ButtonActionCommand>().SingleInstance();
        container.RegisterType<TeamPlayClickToJoinCommand>().As<UserConnectEventCommand>().SingleInstance();
        container.RegisterType<TeamPlayTeammateJoinCommand>().As<UserConnectEventCommand>().SingleInstance();
        container.RegisterType<TeamPlayTeammateLeaveCommand>().As<UserDisconnectEventCommand>().SingleInstance();
        container.RegisterType<TeamPlayRoomOwnerLeaveCommand>().As<UserDisconnectEventCommand>().SingleInstance();

        // Role Command
        container.RegisterType<RoleSettingCommand>().As<GuildMessageCommand>().SingleInstance();

        // Help Command
        container.RegisterType<GlobalHelpCommand>().As<GuildMessageCommand>().SingleInstance();

        // Text Channel Group Control Command
        container.RegisterType<TcGroupControlCommand>().As<GuildMessageCommand>().SingleInstance();

        // Guild Setting Command
        container.RegisterType<GuildCustomFeatureCommand>().As<GuildMessageCommand>().SingleInstance();
        container.RegisterType<SynergyBotAccountCommand>().As<GuildMessageCommand>().SingleInstance();
        container.RegisterType<SynergyBotConflictResolveCommand>().As<GuildMessageCommand>().SingleInstance();
        container.RegisterType<InitSettingsForNewGuildCommand>().As<BotJoinGuildCommand>().SingleInstance();
        container.RegisterType<InitSettingOnConnectUnregisteredGuildCommand>().As<GuildAvailableCommand>()
            .SingleInstance();

        // Splatoon: Suite Search Command
        container.RegisterType<SuiteSearchService>().SingleInstance();
        container.RegisterType<SuiteSearchCommand>().As<GuildMessageCommand>().SingleInstance();

        // Countdown-Name Channel Command
        container.RegisterType<CountDownChannelService>().SingleInstance();
        container.RegisterType<CountDownScanJob>().SingleInstance();
        container.RegisterType<CountDownManageCommand>().As<GuildMessageCommand>().SingleInstance();

        // Clock-In Command
        container.RegisterType<ClockInManageService>().SingleInstance();
        container.RegisterType<ClockInStageManageService>().SingleInstance();
        container.RegisterType<UserClockInService>().SingleInstance();
        container.RegisterType<ClockInMessageSyncJob>().SingleInstance();
        container.RegisterType<ClockInStageQualifiedRoleSyncJob>().SingleInstance();
        container.RegisterType<ClockInStageScanJob>().SingleInstance();
        container.RegisterType<ClockInBuffer>().SingleInstance();
        container.RegisterType<ClockInStageManageCommand>().As<GuildMessageCommand>().SingleInstance();
        container.RegisterType<ClockInManageCommand>().As<GuildMessageCommand>().SingleInstance();
        container.RegisterType<UserClockInCommand>().As<GuildMessageCommand>().SingleInstance();
        container.RegisterType<ClockInCardAction>().As<ButtonActionCommand>().SingleInstance();

#if DEBUG
        // Board Command
        // container.RegisterType<BoardService>().SingleInstance();
        // container.RegisterType<BoardScanJob>().SingleInstance();
        // container.RegisterType<BoardManageCommand>().As<GuildMessageCommand>().SingleInstance();
        // container.RegisterType<CreateSimpleScoreBoardCommand>().As<GuildMessageCommand>().SingleInstance();
#endif
    }
}
