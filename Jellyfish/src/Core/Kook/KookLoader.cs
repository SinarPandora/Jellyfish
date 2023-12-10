using Jellyfish.Core.Config;
using Jellyfish.Core.Job;
using Jellyfish.Module.GuildSetting.Core;
using Kook;
using Kook.WebSocket;

namespace Jellyfish.Core.Kook;

public class KookLoader(
    KookEventMatcher matcher,
    AppConfig appConfig,
    KookSocketClient client,
    ILogger<KookLoader> log,
    GuildSettingService guildSettingService,
    JobLoader jobLoader)
{
    /// <summary>
    ///     Login Kook client
    /// </summary>
    public async Task Login()
    {
        client.Log += KookLog;
        client.Ready += KookReady;
        await client.LoginAsync(TokenType.Bot, appConfig.KookToken);
        await client.StartAsync();
    }

    /// <summary>
    ///     Register commands and actions
    /// </summary>
    private void RegisterActions()
    {
        client.MessageReceived += matcher.OnMessageReceived;
        client.MessageButtonClicked += matcher.OnCardActionClicked;
        client.DirectMessageReceived += matcher.OnDirectMessageReceived;
        client.UserConnected += matcher.OnUserConnected;
        client.UserDisconnected += matcher.OnUserDisconnected;
        client.JoinedGuild += matcher.OnBotJoinGuild;
    }

    /// <summary>
    ///     Kook Client Logger
    /// </summary>
    /// <param name="msg">Message to Log</param>
    /// <returns>Empty Async Result</returns>
    private Task KookLog(LogMessage msg)
    {
        log.LogInformation("{Message}", msg.ToString());
        return Task.CompletedTask;
    }

    private async Task KookReady()
    {
        log.LogInformation("{ClientCurrentUser} 登录成功！", client.CurrentUser);
        await guildSettingService.InitGuildSettings();
        RegisterActions();
        jobLoader.Load();
        log.LogInformation("{ClientCurrentUser} 已就绪！", client.CurrentUser);
    }


    /// <summary>
    ///     Create socket client
    /// </summary>
    /// <param name="config">App Configuration</param>
    /// <returns>Configured socket client</returns>
    public static KookSocketClient CreateSocketClient(AppConfig config)
    {
        return new KookSocketClient(new KookSocketConfig
        {
            AlwaysDownloadUsers = true,
            AlwaysDownloadVoiceStates = true,
            AlwaysDownloadBoostSubscriptions = true,
            MessageCacheSize = 100,
            LogLevel = config.KookEnableDebug ? LogSeverity.Debug : LogSeverity.Info,
            ConnectionTimeout = config.KookConnectTimeout,
            HandlerTimeout = 5_000
        });
    }
}
