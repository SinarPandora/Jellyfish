using Kook;

namespace Jellyfish.Util;

/// <summary>
///     Simple card factory
/// </summary>
public static class SimpleCard
{
    public static Task<Cacheable<IUserMessage, Guid>> SendWarningCardAsync(this IMessageChannel channel,
        string message) => channel.SendCardAsync(IconMessage("⚠️", message));

    public static Task<Cacheable<IUserMessage, Guid>> SendInfoCardAsync(this IMessageChannel channel, string message) =>
        channel.SendCardAsync(IconMessage("💬", message));

    public static Task<Cacheable<IUserMessage, Guid>>
        SendSuccessCardAsync(this IMessageChannel channel, string message) =>
        channel.SendCardAsync(IconMessage("✅", message));

    public static Task<Cacheable<IUserMessage, Guid>>
        SendErrorCardAsync(this IMessageChannel channel, string message) =>
        channel.SendCardAsync(IconMessage("❌", message));

    public static Task<Cacheable<IUserMessage, Guid>>
        SendFatalCardAsync(this IMessageChannel channel, string message) =>
        channel.SendCardAsync(IconMessage("😱", message));

    public static Task<Cacheable<IUserMessage, Guid>> SendMarkdownCardAsync(this IMessageChannel channel,
        string title, string markdown) => channel.SendCardAsync(
        MarkdownMessage($"""
                         **{title}**
                         ---
                         {markdown}
                         """));

    public static Task<Cacheable<IUserMessage, Guid>> SendMarkdownCardAsync(this IMessageChannel channel,
        string markdown) => channel.SendCardAsync(MarkdownMessage(markdown));

    private static Card IconMessage(string icon, string message) => MarkdownMessage($"{icon} {message}");

    private static Card MarkdownMessage(string markdown) =>
        new CardBuilder()
            .AddModule<SectionModuleBuilder>(s => { s.WithText(markdown); })
            .Build();
}
