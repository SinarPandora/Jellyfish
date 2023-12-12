using Jellyfish.Core.Config;
using Kook;
using Kook.WebSocket;

namespace Jellyfish.Core.Kook;

public class KookLoader(
    KookEventMatcher matcher,
    AppConfig appConfig,
    KookSocketClient client,
    ILogger<KookLoader> log)
{
    /// <summary>
    ///     Login Kook client
    /// </summary>
    public async Task Login()
    {
        client.Log += KookLog;
        client.Ready += KookReady;
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

    private Task KookReady()
    {
        log.LogInformation("{ClientCurrentUser} 登录成功！", client.CurrentUser);
        return Task.CompletedTask;
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
