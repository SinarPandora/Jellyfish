using JetBrains.Annotations;
using Refit;

namespace Jellyfish.Client.XScoreList.Response;

/// <summary>
///     X-Score submit response
/// </summary>
[UsedImplicitly]
public class XScoreSubmitResponse
{
    [AliasAs("user_id")] public int UserId { get; set; }
    [AliasAs("score")] public double Score { get; set; }
}
