using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfish.Module.ClockIn.Data;

/// <summary>
///     User clock-in history
/// </summary>
public class ClockInHistory(long configId, ulong userId, ulong channelId)
{
    public Guid Id { get; set; }
    public long ConfigId { get; set; } = configId;
    public ulong UserId { get; set; } = userId;
    public ulong ChannelId { get; set; } = channelId;
    [Column(TypeName = "timestamp")] public DateTime CreateTime { get; set; } = DateTime.Now;

    // References
    public ClockInConfig Config { get; set; } = null!;
}
