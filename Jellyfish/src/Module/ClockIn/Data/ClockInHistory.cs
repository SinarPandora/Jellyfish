using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfish.Module.ClockIn.Data;

/// <summary>
///     User clock-in history
/// </summary>
public class ClockInHistory(long configId, ulong userId, ulong channelId)
{
    public Guid Id { get; set; }
    public long ConfigId { get; init; } = configId;
    public ulong UserId { get; init; } = userId;
    public ulong ChannelId { get; init; } = channelId;
    [Column(TypeName = "timestamp")] public DateTime CreateTime { get; init; } = DateTime.Now;

    // References
    public ClockInConfig Config { get; set; } = null!;
}
