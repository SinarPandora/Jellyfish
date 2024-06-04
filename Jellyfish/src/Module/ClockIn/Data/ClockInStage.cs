using System.ComponentModel.DataAnnotations.Schema;
using Jellyfish.Core.Data;

namespace Jellyfish.Module.ClockIn.Data;

/// <summary>
///     Stage for clock-in.
///     For example, Starting today, users can receive prizes by clocking in for 30 cumulative days!
///                 (Regardless of how many days you have clocked in before)
///     This actually validation rule, so a scanning task is needed to handle it.
/// </summary>
public class ClockInStage(long configId, string name, DateOnly startDate, uint days) : TrackableEntity
{
    public long Id { get; set; }
    public long ConfigId { get; init; } = configId;
    public string Name { get; set; } = name;
    public DateOnly StartDate { get; set; } = startDate;
    public DateOnly? EndDate { get; set; }
    public uint Days { get; set; } = days;
    public uint AllowBreakDays { get; set; }
    public string? QualifiedMessage { get; set; }
    public uint? QualifiedRoleId { get; set; }
    public bool Enabled { get; set; }
    [Column(TypeName = "timestamp")] public DateTime LastScanTime { get; set; } = DateTime.MinValue;

    // References
    public ClockInConfig Config { get; set; } = null!;
    public ICollection<ClockInStageQualifiedHistory> QualifiedHistories { get; set; } = null!;
}
