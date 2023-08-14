using Kook;
using Kook.WebSocket;

namespace Jellyfish.Core;

// ReSharper disable once ClassNeverInstantiated.Global
public class EventMatcher
{
    /// <summary>
    ///     Handle message received event
    /// </summary>
    /// <param name="msg">User message</param>
    /// <param name="user">Current user</param>
    /// <param name="channel">Current channel</param>
    public async Task OnMessageReceived(SocketMessage msg, SocketGuildUser user, SocketTextChannel channel)
    {
        if (msg is { Type: MessageType.KMarkdown, Content: "Hello" })
        {
            await channel.SendTextAsync("Hello world!");
        }
    }
}
