using Jellyfish.Core.Enum;

namespace Jellyfish.Module.ExpireExtendSession.Data;

/// <summary>
///     Channel extend session
/// </summary>
public class ExpireExtendSession(long targetId, ExtendTargetType targetType, uint value, TimeUnit timeUnit)
{
    public long Id { get; set; }
    public long TargetId { get; set; } = targetId;
    public ExtendTargetType TargetType { get; set; } = targetType;
    public uint Value { get; set; } = value;
    public TimeUnit TimeUnit { get; set; } = timeUnit;
}
