using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfish.Module.Push.Weibo.Data;

/// <summary>
///     History to record which channel the bot pushed
/// </summary>
public class WeiboPushHistory(long instanceId, string hash)
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long InstanceId { get; init; } = instanceId;
    public string Hash { get; init; } = hash;
    [Column(TypeName = "timestamp")] public DateTime CreateTime { get; init; } = DateTime.Now;

    // References
    public WeiboPushInstance Instance { get; set; } = null!;
}
