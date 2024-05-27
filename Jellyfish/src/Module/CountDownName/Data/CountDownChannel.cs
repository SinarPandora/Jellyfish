using Jellyfish.Core.Data;

namespace Jellyfish.Module.CountDownName.Data;

/// <summary>
///     CountDown-Name channel
/// </summary>
public class CountDownChannel(
    ulong guildId,
    ulong channelId,
    string pattern,
    DateOnly dueDate
) : TrackableEntity
{
    public long Id { get; set; }
    public ulong GuildId { get; set; } = guildId;
    public ulong ChannelId { get; set; } = channelId;
    public string Pattern { get; set; } = pattern;
    public DateOnly DueDate { get; set; } = dueDate;
    public string? DueText { get; set; } = null;
}
