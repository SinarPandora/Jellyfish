using Jellyfish.Core.Enum;

namespace Jellyfish.Module.ExpireExtendSession.Data;

/// <summary>
///     Channel extend session
/// </summary>
public class ExpireExtendSession(
    long targetId,
    ExtendTargetType targetType,
    uint value,
    TimeUnit timeUnit
)
{
    public long Id { get; set; }
    public long TargetId { get; init; } = targetId;
    public ExtendTargetType TargetType { get; init; } = targetType;
    public uint Value { get; init; } = value;
    public TimeUnit TimeUnit { get; init; } = timeUnit;
}
