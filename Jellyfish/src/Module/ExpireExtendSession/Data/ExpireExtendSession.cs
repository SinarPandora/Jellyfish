using Jellyfish.Core.Enum;

namespace Jellyfish.Module.ExpireExtendSession.Data;

/// <summary>
///     Channel extend session
/// </summary>
public class ExpireExtendSession
{
    public ExpireExtendSession(long targetId, ExtendTargetType targetType, uint value, TimeUnit timeUnit)
    {
        TargetId = targetId;
        TargetType = targetType;
        Value = value;
        TimeUnit = timeUnit;
    }

    public long Id { get; set; }
    public long TargetId { get; set; }
    public ExtendTargetType TargetType { get; set; }
    public uint Value { get; set; }
    public TimeUnit TimeUnit { get; set; }
}
