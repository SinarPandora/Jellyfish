namespace Jellyfish.Module.ClockIn.Data;

/// <summary>
///     Living clock-in card instance
/// </summary>
public class ClockInCardInstance(long configId, ulong channelId, Guid messageId)
{
    public long Id { get; set; }
    public long ConfigId { get; init; } = configId;
    public ulong ChannelId { get; init; } = channelId;
    public Guid MessageId { get; set; } = messageId;

    // References
    public ClockInConfig Config { get; set; } = null!;
}
