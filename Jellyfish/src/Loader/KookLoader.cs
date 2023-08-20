using Jellyfish.Config;
using Jellyfish.Core;
using Kook;
using Kook.WebSocket;
using NLog;

namespace Jellyfish.Loader;

public class KookLoader
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
}
