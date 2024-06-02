using Jellyfish.Core.Data;

namespace Jellyfish.Module.ClockIn.Data;

/// <summary>
///     The cumulative clock-in status of a user
///     which is used as a cache to speed up the generation of leaderboards
/// </summary>
public class UserClockInStatus(long configId, ulong userId, string username) : TrackableEntity
{
    public long Id { get; set; }
    public long ConfigId { get; init; } = configId;
    public ulong UserId { get; init; } = userId;
    public string Username { get; set; } = username;
    public uint AllClockInCount { get; set; }
    public bool IsClockInToday { get; set; }
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    // References
    public ClockInConfig Config { get; set; } = null!;
    public ICollection<ClockInStageQualifiedHistory> QualifiedHistories { get; set; } = null!;
}
