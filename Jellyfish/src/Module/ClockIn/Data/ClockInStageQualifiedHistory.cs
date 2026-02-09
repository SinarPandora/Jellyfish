using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfish.Module.ClockIn.Data;

/// <summary>
///     History for user qualified
/// </summary>
public class ClockInStageQualifiedHistory(long stageId, long userStatusId, uint? givenRoleId)
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long StageId { get; init; } = stageId;
    public long UserStatusId { get; init; } = userStatusId;
    public uint? GivenRoleId { get; set; } = givenRoleId;

    [Column(TypeName = "timestamp")]
    public DateTime CreateTime { get; init; } = DateTime.Now;

    // References
    public ClockInStage Stage { get; set; } = null!;
    public UserClockInStatus UserStatus { get; set; } = null!;
}
