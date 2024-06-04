using Jellyfish.Core.Data;

namespace Jellyfish.Module.CountDownName.Data;

/// <summary>
///     CountDown-Name channel
///     Can also be used for positive countdown,
///     And when due date equals with the creation date, the countdown is also positive
/// </summary>
public class CountDownChannel(
    ulong guildId,
    ulong channelId,
    string pattern,
    DateOnly dueDate,
    bool positive
) : TrackableEntity
{
    public long Id { get; set; }
    public ulong GuildId { get; init; } = guildId;
    public ulong ChannelId { get; init; } = channelId;
    public string Pattern { get; init; } = pattern;
    public DateOnly DueDate { get; init; } = dueDate;
    public bool Positive { get; init; } = positive;
    public string? DueText { get; set; }
}
