using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfish.Module.Push.Weibo.Data;

/// <summary>
///     History to record which channel the bot pushed
/// </summary>
public class WeiboPushHistory(long instanceId, string hash, string mid, Guid messageId)
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long InstanceId { get; init; } = instanceId;
    public string Hash { get; set; } = hash;
    public string Mid { get; set; } = mid;
    public Guid MessageId { get; set; } = messageId;
    [Column(TypeName = "timestamp")] public DateTime CreateTime { get; init; } = DateTime.Now;

    // References
    public WeiboPushInstance Instance { get; set; } = null!;
}
