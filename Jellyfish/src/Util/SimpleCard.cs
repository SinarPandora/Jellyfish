using Kook;

namespace Jellyfish.Util;

/// <summary>
///     Simple card factory
/// </summary>
public static class SimpleCard
{
    /// <summary>
    ///     Send a warning card message for notification
    /// </summary>
    /// <param name="channel">Text channel</param>
    /// <param name="message">Card text</param>
    /// <param name="recall">Is recall after timeout?</param>
    /// <param name="timeout">Recall timeout</param>
    /// <returns>The card message</returns>
    public static Task<Cacheable<IUserMessage, Guid>?> SendWarningCardAsync(
        this IMessageChannel channel,
        string message,
        bool recall,
        TimeSpan? timeout = null
    ) => SendNotifyCardAsync(channel, message, "⚠️", Color.Orange, recall, timeout);

    /// <summary>
    ///     Send an info card message for notification
    /// </summary>
    /// <param name="channel">Text channel</param>
    /// <param name="message">Card text</param>
    /// <param name="recall">Is recall after timeout?</param>
    /// <param name="timeout">Recall timeout</param>
    /// <returns>The card message</returns>
    public static Task<Cacheable<IUserMessage, Guid>?> SendInfoCardAsync(
        this IMessageChannel channel,
        string message,
        bool recall,
        TimeSpan? timeout = null
    ) => SendNotifyCardAsync(channel, message, "💬", Color.Blue, recall, timeout);

    /// <summary>
    ///     Send a success card message for notification
    /// </summary>
    /// <param name="channel">Text channel</param>
    /// <param name="message">Card text</param>
    /// <param name="recall">Is recall after timeout?</param>
    /// <param name="timeout">Recall timeout</param>
    /// <returns>The card message</returns>
    public static Task<Cacheable<IUserMessage, Guid>?> SendSuccessCardAsync(
        this IMessageChannel channel,
        string message,
        bool recall,
        TimeSpan? timeout = null
    ) => SendNotifyCardAsync(channel, message, "✅", Color.Green, recall, timeout);

    /// <summary>
    ///     Send an error card message for notification
    /// </summary>
    /// <param name="channel">Text channel</param>
    /// <param name="message">Card text</param>
    /// <param name="recall">Is recall after timeout?</param>
    /// <param name="timeout">Recall timeout</param>
    /// <returns>The card message</returns>
    public static Task<Cacheable<IUserMessage, Guid>?> SendErrorCardAsync(
        this IMessageChannel channel,
        string message,
        bool recall,
        TimeSpan? timeout = null
    ) => SendNotifyCardAsync(channel, message, "❌", Color.Red, recall, timeout);

    /// <summary>
    ///     Send fatal card message for notification
    /// </summary>
    /// <param name="channel">Text channel</param>
    /// <param name="message">Card text</param>
    /// <param name="recall">Is recall after timeout?</param>
    /// <param name="timeout">Recall timeout</param>
    /// <returns>The card message</returns>
    public static Task<Cacheable<IUserMessage, Guid>?> SendFatalCardAsync(
        this IMessageChannel channel,
        string message,
        bool recall,
        TimeSpan? timeout = null
    ) => SendNotifyCardAsync(channel, message, "😱", Color.Red, recall, timeout);

    private static Task<Cacheable<IUserMessage, Guid>?> SendNotifyCardAsync(
        IMessageChannel channel,
        string message,
        string icon,
        Color color,
        bool recall,
        TimeSpan? timeout = null
    ) =>
        recall
            ? channel.SendAutoRecallCardAsync(
                IconMessage(
                    icon,
                    message,
                    color,
                    timeout ?? AutoRecallMessageHelper.DefaultRecallTimeout
                ),
                timeout ?? AutoRecallMessageHelper.DefaultRecallTimeout
            )
            : channel.SendCardSafeAsync(IconMessage(icon, message, color));

    private static Card IconMessage(
        string icon,
        string message,
        Color color,
        TimeSpan? timeout = null
    ) => MarkdownMessage($"{icon} {message}", color, timeout);

    private static Card MarkdownMessage(string markdown, Color color, TimeSpan? timeout = null)
    {
        var cb = new CardBuilder()
            .AddModule<SectionModuleBuilder>(s =>
            {
                s.WithText(markdown, true);
            })
            .WithColor(color);

        // Add optional timeout
        if (timeout.HasValue)
        {
            cb.AddModule<CountdownModuleBuilder>(m =>
            {
                m.Mode = CountdownMode.Second;
                m.EndTime = DateTimeOffset.Now.Add(timeout.Value);
            });
        }

        return cb.Build();
    }
}
