using Kook;

namespace Jellyfish.Util;

/// <summary>
///     Simple card factory
/// </summary>
public static class SimpleCard
{
    /// <summary>
    ///     Send warning card message for notification
    /// </summary>
    /// <param name="channel">Text channel</param>
    /// <param name="message">Card text</param>
    /// <param name="recall">Is recall after timeout?</param>
    /// <param name="timeout">Recall timeout</param>
    public static Task SendWarningCardAsync(this IMessageChannel channel,
        string message, bool recall, TimeSpan? timeout = null) =>
        recall
            ? channel.SendAutoRecallCardAsync(
                IconMessage("⚠️", message, Color.Orange, timeout ?? AutoRecallMessageHelper.DefaultRecallTimeout),
                AutoRecallMessageHelper.DefaultRecallTimeout)
            : channel.SendCardAsync(IconMessage("⚠️", message, Color.Orange));

    /// <summary>
    ///     Send info card message for notification
    /// </summary>
    /// <param name="channel">Text channel</param>
    /// <param name="message">Card text</param>
    /// <param name="recall">Is recall after timeout?</param>
    /// <param name="timeout">Recall timeout</param>
    public static Task SendInfoCardAsync(this IMessageChannel channel, string message,
        bool recall, TimeSpan? timeout = null) =>
        recall
            ? channel.SendAutoRecallCardAsync(
                IconMessage("💬", message, Color.Blue, timeout ?? AutoRecallMessageHelper.DefaultRecallTimeout),
                AutoRecallMessageHelper.DefaultRecallTimeout)
            : channel.SendCardAsync(IconMessage("💬", message, Color.Blue));

    /// <summary>
    ///     Send success card message for notification
    /// </summary>
    /// <param name="channel">Text channel</param>
    /// <param name="message">Card text</param>
    /// <param name="recall">Is recall after timeout?</param>
    /// <param name="timeout">Recall timeout</param>
    public static Task SendSuccessCardAsync(this IMessageChannel channel, string message,
        bool recall, TimeSpan? timeout = null) =>
        recall
            ? channel.SendAutoRecallCardAsync(
                IconMessage("✅", message, Color.Green, timeout ?? AutoRecallMessageHelper.DefaultRecallTimeout),
                AutoRecallMessageHelper.DefaultRecallTimeout)
            : channel.SendCardAsync(IconMessage("✅", message, Color.Green));

    /// <summary>
    ///     Send error card message for notification
    /// </summary>
    /// <param name="channel">Text channel</param>
    /// <param name="message">Card text</param>
    /// <param name="recall">Is recall after timeout?</param>
    /// <param name="timeout">Recall timeout</param>
    public static Task SendErrorCardAsync(this IMessageChannel channel, string message,
        bool recall, TimeSpan? timeout = null) =>
        recall
            ? channel.SendAutoRecallCardAsync(
                IconMessage("❌", message, Color.Red, timeout ?? AutoRecallMessageHelper.DefaultRecallTimeout),
                AutoRecallMessageHelper.DefaultRecallTimeout)
            : channel.SendCardAsync(IconMessage("❌", message, Color.Red));

    /// <summary>
    ///     Send fatal card message for notification
    /// </summary>
    /// <param name="channel">Text channel</param>
    /// <param name="message">Card text</param>
    /// <param name="recall">Is recall after timeout?</param>
    /// <param name="timeout">Recall timeout</param>
    public static Task SendFatalCardAsync(this IMessageChannel channel, string message,
        bool recall, TimeSpan? timeout = null) =>
        recall
            ? channel.SendAutoRecallCardAsync(
                IconMessage("😱", message, Color.Red, timeout ?? AutoRecallMessageHelper.DefaultRecallTimeout),
                AutoRecallMessageHelper.DefaultRecallTimeout)
            : channel.SendCardAsync(IconMessage("😱", message, Color.Red));


    private static Card IconMessage(string icon, string message, Color color, TimeSpan? timeout = null) =>
        MarkdownMessage($"{icon} {message}", color, timeout);

    private static Card MarkdownMessage(string markdown, Color color, TimeSpan? timeout = null)
    {
        var cb = new CardBuilder()
            .AddModule<SectionModuleBuilder>(s => { s.WithText(markdown, true); })
            .WithColor(color);

        // Add optional timeout
        if (timeout != null)
        {
            cb.AddModule<CountdownModuleBuilder>(m =>
            {
                m.Mode = CountdownMode.Second;
                m.EndTime = DateTimeOffset.Now.Add((TimeSpan)timeout);
            });
        }

        return cb.Build();
    }
}
