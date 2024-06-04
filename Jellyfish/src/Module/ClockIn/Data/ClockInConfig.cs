using Jellyfish.Core.Data;

namespace Jellyfish.Module.ClockIn.Data;

/// <summary>
///     Config for the clock-in
/// </summary>
public class ClockInConfig(ulong guildId) : TrackableEntity
{
    public long Id { get; set; }
    public ulong GuildId { get; init; } = guildId;
    public string Title { get; set; } = "每日打卡";
    public string? Description { get; set; }
    public string ButtonText { get; set; } = "打卡！";
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     All stage results will be sent to this channel
    /// </summary>
    public ulong? ResultChannelId { get; set; }

    public uint TodayClockInCount { get; set; }
    public uint AllClockInCount { get; set; }

    // References
    public ICollection<UserClockInStatus> UserStatuses { get; set; } = null!;
    public ICollection<ClockInCardInstance> CardInstances { get; set; } = null!;
    public ICollection<ClockInStage> Stages { get; set; } = null!;
    public ICollection<ClockInHistory> Histories { get; set; } = null!;
}
