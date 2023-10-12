using Jellyfish.Core.Config;
using Kook;
using Kook.WebSocket;

namespace Jellyfish.Core.Kook;

public class KookLoader
{
    private readonly ILogger<KookLoader> _log;

    private readonly AppConfig _appConfig;
    private readonly KookSocketClient _client;
    private readonly KookEventMatcher _kookEventMatcher;

    public KookLoader(KookEventMatcher matcher, AppConfig appConfig, KookSocketClient client, ILogger<KookLoader> log)
    {
        _kookEventMatcher = matcher;
        _appConfig = appConfig;
        _client = client;
        _log = log;
    }

    public async Task Load()
    {
        _client.Log += KookLog;
        _client.Ready += KookReady;
        _client.MessageReceived += _kookEventMatcher.OnMessageReceived;
        _client.MessageButtonClicked += _kookEventMatcher.OnCardActionClicked;
        _client.DirectMessageReceived += _kookEventMatcher.OnDirectMessageReceived;
        _client.UserConnected += _kookEventMatcher.OnUserConnected;
        _client.UserDisconnected += _kookEventMatcher.OnUserDisconnected;
        await _client.LoginAsync(TokenType.Bot, _appConfig.KookToken);
        await _client.StartAsync();
    }

    /// <summary>
    ///     Kook Client Logger
    /// </summary>
    /// <param name="msg">Message to Log</param>
    /// <returns>Empty Async Result</returns>
    private Task KookLog(LogMessage msg)
    {
        _log.LogInformation("{Message}", msg.ToString());
        return Task.CompletedTask;
    }

    private Task KookReady()
    {
        _log.LogInformation("{ClientCurrentUser} 已连接！", _client.CurrentUser);
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
