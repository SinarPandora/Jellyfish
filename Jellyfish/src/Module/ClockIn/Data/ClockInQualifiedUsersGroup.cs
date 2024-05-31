using Jellyfish.Core.Data;

namespace Jellyfish.Module.ClockIn.Data;

/// <summary>
///     Cache for qualified user
/// </summary>
public class ClockInQualifiedUser(long configId, long stageId, ulong userId) : TrackableEntity
{
    public Guid Id { get; set; }
    public long ConfigId { get; init; } = configId;
    public long StageId { get; init; } = stageId;
    public ulong UserId { get; init; } = userId;

    // References
    public ClockInConfig Config { get; set; } = null!;
    public ClockInStage Stage { get; set; } = null!;
}
