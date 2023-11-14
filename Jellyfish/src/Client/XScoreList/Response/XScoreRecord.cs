using JetBrains.Annotations;
using Refit;

namespace Jellyfish.Client.XScoreList.Response;

/// <summary>
///     Full X-Score record
/// </summary>
[UsedImplicitly]
public class XScoreRecord
{
    [AliasAs("user_id")] public int UserId { get; set; }
    [AliasAs("nickname")] public string SplatoonName { get; set; } = null!;
    [AliasAs("nicknum")] public int SplatoonId { get; set; }
    [AliasAs("kook_name")] public string KookName { get; set; } = null!;
    [AliasAs("score")] public double Score { get; set; }
    [AliasAs("valid")] public bool Valid { get; set; }
    [AliasAs("cancelled")] public bool Cancelled { get; set; }
    [AliasAs("submit_time")] public DateTime SubmitTime { get; set; }
}
