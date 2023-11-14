using JetBrains.Annotations;
using Refit;

namespace Jellyfish.Client.XScoreList.Response;

/// <summary>
///     X-Score count
/// </summary>
[UsedImplicitly]
public class XScoreCount
{
    [AliasAs("count")] public int Count { get; set; }
}
