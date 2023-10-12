using FluentScheduler;
using Jellyfish.Core.Cache;
using Jellyfish.Core.Command;
using Jellyfish.Core.Config;
using Jellyfish.Core.Job;
using Jellyfish.Core.Kook;
using Jellyfish.Core.Lifecycle;
using Jellyfish.Module;
using Jellyfish.Module.GroupControl;
using Jellyfish.Module.Help;
using Jellyfish.Module.Role;
using Jellyfish.Module.TeamPlay;
using Jellyfish.Module.TeamPlay.Core;
using Jellyfish.Module.TeamPlay.Job;
using Jellyfish.Module.TeamPlay.Manage;
using Jellyfish.Module.TeamPlay.User;
using Kook.WebSocket;

namespace Jellyfish.Core.Container;

/// <summary>
///     The application context, binding all instance which application needs
/// </summary>
public static class AppContext
{
    public static void BindAll(IServiceCollection container)
    {
        // ------------------------------------------------ System -----------------------------------------------------
        container.AddScoped<CacheLoader>();
        container.AddSingleton<AppConfig>();
        container.AddSingleton<KookSocketClient>(provider =>
            KookLoader.CreateSocketClient(provider.GetRequiredService<AppConfig>()));
        container.AddScoped<KookLoader>();
        container.AddSingleton<KookApiFactory>();
        container.AddScoped<KookEventMatcher>();
        container.AddScoped<CacheSyncJob>();
        container.AddScoped<Registry, JobRegistry>();
        container.AddScoped<JobLoader>();
        container.AddTransient<IStartupFilter, AppInitializer>();

        // ------------------------------------------------ Commands ---------------------------------------------------
        // Simple Command
        container.AddScoped<GuildMessageCommand, SimpleTestCommand>();

        // TeamPlay Command
        container.AddScoped<TeamPlayRoomScanJob>();
        container.AddScoped<TeamPlayRoomService>();
        container.AddScoped<TeamPlayManageService>();
        container.AddScoped<GuildMessageCommand, TeamPlayManageCommand>();
        container.AddScoped<GuildMessageCommand, TeamPlayUserCommand>();
        container.AddScoped<ButtonActionCommand, TeamPlayButtonActionEntry>();
        container.AddScoped<UserConnectEventCommand, TeamPlayClickToJoinCommand>();
        container.AddScoped<UserDisconnectEventCommand, TeamPlayRoomOwnerLeaveCommand>();

        // Role Command
        container.AddScoped<GuildMessageCommand, RoleSettingCommand>();

        // Help Command
        container.AddScoped<GuildMessageCommand, GlobalHelpCommand>();

        // Text Channel Group Control Command
        container.AddScoped<GuildMessageCommand, TcGroupControlCommand>();
    }
}
