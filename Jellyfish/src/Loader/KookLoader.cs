using AutoDI;
using Jellyfish.Config;
using Jellyfish.Core;
using Kook;
using Kook.Rest;
using Kook.WebSocket;
using NLog;

namespace Jellyfish.Loader;

public class KookLoader
{
    public static readonly KookLoader Instance = new();

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly AppConfig _appConfig;
    private readonly KookSocketClient _client;
    private readonly EventMatcher _eventMatcher;

    private KookLoader(
        [Dependency] EventMatcher? matcher = null,
        [Dependency] AppConfig? appConfig = null,
        [Dependency] KookSocketClient? client = null
    )
    {
        _eventMatcher = matcher ?? throw new ArgumentNullException(nameof(matcher));
        _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task Boot()
    {
        _client.Log += Log;

        _client.MessageReceived += _eventMatcher.OnMessageReceived;
        await _client.LoginAsync(TokenType.Bot, _appConfig.KookToken);
        await _client.StartAsync();
    }

    /// <summary>
    ///     Kook Client Logger
    /// </summary>
    /// <param name="msg">Message to Log</param>
    /// <returns>Empty Async Result</returns>
    private static Task Log(LogMessage msg)
    {
        Logger.Info(msg.ToString);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Kook Restful API Client
    /// </summary>
    /// <returns>Logged-in API Client</returns>
    public async Task<KookRestClient> CreateApiClient()
    {
        var apiClient = new KookRestClient();
        await apiClient.LoginAsync(TokenType.Bot, _appConfig.KookToken);
        return apiClient;
    }
}
