using Jellyfish.Config;
using Jellyfish.Core;
using Kook;
using Kook.WebSocket;
using NLog;

namespace Jellyfish.Loader;

public class KookLoader
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly AppConfig _appConfig;
    private readonly KookSocketClient _client;
    private readonly EventMatcher _eventMatcher;

    public KookLoader(EventMatcher matcher, AppConfig appConfig, KookSocketClient client)
    {
        _eventMatcher = matcher;
        _appConfig = appConfig;
        _client = client;
    }

    public async Task Boot()
    {
        _client.Log += KookLog;
        _client.Ready += KookReady;
        _client.MessageReceived += _eventMatcher.OnMessageReceived;
        _client.MessageButtonClicked += _eventMatcher.OnCardActionClicked;
        await _client.LoginAsync(TokenType.Bot, _appConfig.KookToken);
        await _client.StartAsync();
    }

    /// <summary>
    ///     Kook Client Logger
    /// </summary>
    /// <param name="msg">Message to Log</param>
    /// <returns>Empty Async Result</returns>
    private static Task KookLog(LogMessage msg)
    {
        Log.Info(msg.ToString);
        return Task.CompletedTask;
    }

    private Task KookReady()
    {
        Log.Info($"{_client.CurrentUser} 已连接！");
        return Task.CompletedTask;
    }


    /// <summary>
    ///     Create socket client
    /// </summary>
    /// <param name="config">App Configuration</param>
    /// <returns>Configured socket client</returns>
    public static KookSocketClient CreateSocketClient(AppConfig config) =>
        new(new KookSocketConfig
        {
            AlwaysDownloadUsers = true,
            AlwaysDownloadVoiceStates = true,
            AlwaysDownloadBoostSubscriptions = true,
            MessageCacheSize = 100,
            LogLevel = config.KookEnableDebug ? LogSeverity.Debug : LogSeverity.Info,
            ConnectionTimeout = config.KookConnectTimeout,
            HandlerTimeout = 10_000
        });
}
