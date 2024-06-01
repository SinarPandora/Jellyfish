using Jellyfish.Core.Data;

namespace Jellyfish.Module.ClockIn.Data;

/// <summary>
///     The cumulative clock-in status of a user
///     which is used as a cache to speed up the generation of leaderboards
/// </summary>
public class UserClockInStatus(long configId, ulong userId) : TrackableEntity
{
    public long Id { get; set; }
    public long ConfigId { get; init; } = configId;
    public ulong UserId { get; init; } = userId;
    public uint AllClockInCount { get; set; }
    public bool IsClockInToday { get; set; }

    // References
    public ClockInConfig Config { get; set; } = null!;
    public ICollection<ClockInStageQualifiedHistory> QualifiedHistories { get; set; } = null!;
}
