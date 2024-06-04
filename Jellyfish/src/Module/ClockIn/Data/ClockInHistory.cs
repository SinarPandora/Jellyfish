using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfish.Module.ClockIn.Data;

/// <summary>
///     User clock-in history
/// </summary>
public class ClockInHistory(long configId, long userStatusId, ulong channelId)
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long ConfigId { get; init; } = configId;
    public long UserStatusId { get; init; } = userStatusId;
    public ulong ChannelId { get; init; } = channelId;
    [Column(TypeName = "timestamp")] public DateTime CreateTime { get; init; } = DateTime.Now;

    // References
    public ClockInConfig Config { get; set; } = null!;
    public UserClockInStatus UserStatus { get; set; } = null!;
}
