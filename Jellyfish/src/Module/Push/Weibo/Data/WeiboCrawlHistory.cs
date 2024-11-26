using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfish.Module.Push.Weibo.Data;

/// <summary>
///     Crawl history for Weibo
/// </summary>
public class WeiboCrawlHistory(string uid, string hash)
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Uid { get; set; } = uid;
    public string Hash { get; set; } = hash;
    [Column(TypeName = "timestamp")] public DateTime CreateTime { get; init; } = DateTime.Now;
}
