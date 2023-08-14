using AutoDI;
using Jellyfish.Core;
using Kook;
using Kook.WebSocket;
using NLog;

namespace Jellyfish.Loader;

public class KookLoader
{
    public static readonly KookLoader Instance = new();

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static readonly KookSocketClient Client = new();

    private readonly EventMatcher _eventMatcher;

    private KookLoader([Dependency] EventMatcher matcher = null!)
    {
        _eventMatcher = matcher;
        if (matcher == null) throw new ArgumentNullException(nameof(matcher));
    }

    public async Task<KookSocketClient> Boot()
    {
        Client.Log += Log;

        Client.MessageReceived += _eventMatcher.OnMessageReceived;
        await Client.LoginAsync(TokenType.Bot, "");
        await Client.StartAsync();

        return Client;
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
