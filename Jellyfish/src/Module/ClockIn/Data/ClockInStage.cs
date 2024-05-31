namespace Jellyfish.Module.ClockIn.Data;

/// <summary>
///     Stage for clock-in.
///     For example, Starting today, users can receive prizes by clocking in for 30 cumulative days!
///                 (Regardless of how many days you have clocked in before)
///     This actually validation rule, so a scanning task is needed to handle it.
/// </summary>
public class ClockInStage(long configId, DateTime startDate, uint days, uint allowBreakDays)
{
    public long Id { get; set; }
    public long ConfigId { get; init; } = configId;
    public DateTime StartDate { get; set; } = startDate;
    public DateTime? EndDate { get; set; }
    public uint Days { get; set; } = days;
    public uint AllowBreakDays { get; set; } = allowBreakDays;
    public string? QualifiedMessagePattern { get; set; }
    public ulong? QualifiedRoleId { get; set; }
    public bool Enabled { get; set; } = true;

    // References
    public ClockInConfig Config { get; set; } = null!;
    public ICollection<ClockInQualifiedUser> QualifiedUsers { get; set; } = null!;
}
