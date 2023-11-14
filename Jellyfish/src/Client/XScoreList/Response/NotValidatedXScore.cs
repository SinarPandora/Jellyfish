using JetBrains.Annotations;
using Refit;

namespace Jellyfish.Client.XScoreList.Response;

/// <summary>
///     Single not validate X-Score record
/// </summary>
[UsedImplicitly]
public class NotValidatedXScore
{
    [AliasAs("user_id")] public int UserId { get; set; }
    [AliasAs("score")] public double Score { get; set; }
    [AliasAs("submit_time")] public DateTime SubmitTime { get; set; }
}
