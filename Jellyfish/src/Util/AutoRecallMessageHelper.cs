using Kook;

namespace Jellyfish.Util;

/// <summary>
///     Help methods for text channel
/// </summary>
public static class AutoRecallMessageHelper
{
    public static readonly TimeSpan DefaultRecallTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Send card to channel, then recall it after reached timeout
    /// </summary>
    /// <param name="channel">Text channel</param>
    /// <param name="card">Card message</param>
    /// <param name="timeout">Recall timeout</param>
    public static async Task SendAutoRecallCardAsync(this IMessageChannel channel, Card card, TimeSpan? timeout = null)
    {
        var result = await channel.SendCardSafeAsync(card);
        if (result.HasValue)
        {
            _ = channel.DeleteMessageWithTimeoutAsync(result.Value.Id, timeout);
        }
    }

    /// <summary>
    ///     Send message to channel, then recall it after reached timeout
    /// </summary>
    /// <param name="channel">Text channel</param>
    /// <param name="message">Text message</param>
    /// <param name="timeout">Recall timeout</param>
    public static async Task SendAutoRecallTextAsync(this IMessageChannel channel, string message,
        TimeSpan? timeout = null)
    {
        var result = await channel.SendTextSafeAsync(message);
        if (result.HasValue)
        {
            _ = channel.DeleteMessageWithTimeoutAsync(result.Value.Id, timeout);
        }
    }

    /// <summary>
    ///     Recall message after reach the timeout
    /// </summary>
    /// <param name="channel">Text channel</param>
    /// <param name="messageId">Message Id</param>
    /// <param name="timeout">Recall timeout</param>
    public static async Task DeleteMessageWithTimeoutAsync(this IMessageChannel channel, Guid messageId,
        TimeSpan? timeout = null)
    {
        await Task.Delay(timeout ?? DefaultRecallTimeout);
        await channel.DeleteMessageAsync(messageId);
    }
}
