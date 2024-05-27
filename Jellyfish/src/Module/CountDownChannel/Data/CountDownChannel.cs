using Jellyfish.Core.Data;

namespace Jellyfish.Module.CountDownChannel.Data;

/// <summary>
///     Countdown name channel
/// </summary>
public class CountDownChannel(
    ulong guildId,
    ulong channelId,
    string pattern,
    DateTime dueDate
) : TrackableEntity
{
    public long Id { get; set; }
    public ulong GuildId { get; set; } = guildId;
    public ulong ChannelId { get; set; } = channelId;
    public string Pattern { get; set; } = pattern;
    public DateTime DueDate { get; set; } = dueDate;
}
