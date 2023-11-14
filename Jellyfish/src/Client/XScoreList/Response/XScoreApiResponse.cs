using JetBrains.Annotations;
using Refit;

namespace Jellyfish.Client.XScoreList.Response;

/// <summary>
///     X-Score API response
/// </summary>
/// <typeparam name="T">Data content</typeparam>
[UsedImplicitly]
public class XScoreApiResponse<T>
{
    [AliasAs("code")] public int Code { get; set; }
    [AliasAs("msg")] public string Msg { get; set; } = null!;
    [AliasAs("data")] public T? Data { get; set; }
}
