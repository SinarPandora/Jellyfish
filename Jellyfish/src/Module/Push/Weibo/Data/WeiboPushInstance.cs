namespace Jellyfish.Module.Push.Weibo.Data;

/// <summary>
///     Weibo-Push instance
///     It is used to record which channel the weibo is push to
/// </summary>
public class WeiboPushInstance(long configId, ulong channelId)
{
    public long Id { get; set; }
    public long ConfigId { get; init; } = configId;
    public ulong ChannelId { get; init; } = channelId;

    // References
    public WeiboPushConfig Config { get; set; } = null!;
    public ICollection<WeiboPushHistory> PushHistories { get; set; } = null!;
}
