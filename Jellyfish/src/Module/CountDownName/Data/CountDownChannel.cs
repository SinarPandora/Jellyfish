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
    public ulong GuildId { get; set; } = guildId;
    public ulong ChannelId { get; set; } = channelId;
    public string Pattern { get; set; } = pattern;
    public DateOnly DueDate { get; set; } = dueDate;
    public bool Positive { get; set; } = positive;
    public string? DueText { get; set; } = null;
}
