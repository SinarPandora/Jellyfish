using Jellyfish.Core.Config;
using Jellyfish.Core.Job;
using Kook;
using Kook.WebSocket;

namespace Jellyfish.Core.Kook;

public class KookLoader(
    KookEventMatcher matcher,
    AppConfig appConfig,
    KookSocketClient client,
    JobLoader jobLoader,
    ILogger<KookLoader> log
)
{
    /// <summary>
    ///     Login Kook client
    /// </summary>
    public async Task Login()
    {
        client.Log += KookLog;
        client.Ready += OnKookReady;
        client.Disconnected += OnKookDisconnected;
        RegisterActions();
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
        client.GuildAvailable += matcher.OnGuildAvailable;
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

    /// <summary>
    ///     Run when the socket client is ready
    /// </summary>
    private Task OnKookReady()
    {
        log.LogInformation("{ClientCurrentUser} 登录成功！", client.CurrentUser);
        jobLoader.Start();
        log.LogInformation("定时任务已启用");
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Run when the socket client is disconnected
    /// </summary>
    /// <param name="error">The error which causes the client disconnected</param>
    private Task OnKookDisconnected(Exception error)
    {
        log.LogWarning(error, "Kook 连接断开！正在尝试重新连接");
        jobLoader.Stop();
        log.LogWarning("定时任务已暂停");
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Create a socket client
    /// </summary>
    /// <param name="config">App Configuration</param>
    /// <returns>Configured socket client</returns>
    public static KookSocketClient CreateSocketClient(AppConfig config)
    {
        return new KookSocketClient(
            new KookSocketConfig
            {
                AlwaysDownloadUsers = true,
                AlwaysDownloadVoiceStates = true,
                AlwaysDownloadBoostSubscriptions = true,
                MessageCacheSize = 100,
                LogLevel = config.KookEnableDebug ? LogSeverity.Debug : LogSeverity.Info,
                ConnectionTimeout = config.KookConnectTimeout,
                StartupCacheFetchMode = StartupCacheFetchMode.Synchronous,
                HandlerTimeout = 5_000,
            }
        );
    }
}
