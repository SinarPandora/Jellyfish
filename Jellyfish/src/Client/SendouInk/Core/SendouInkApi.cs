using Jellyfish.Client.SendouInk.Response;
using Refit;

namespace Jellyfish.Client.SendouInk.Core;

/// <summary>
///     Sendou Ink site API
/// </summary>
[Headers("accept-language: zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6")]
public interface ISendouInkApi
{
    /// <summary>
    ///     Get suite builds
    /// </summary>
    /// <param name="weaponSlug">Weapon slug</param>
    /// <param name="filterJsonStr">Build filter in json string</param>
    /// <param name="limit">Per page limit, 24 as first page, 48 to load 2 page together, and so on</param>
    /// <param name="data">Query data</param>
    /// <returns>Suite builds</returns>
    [Get("/builds/{slug}")]
    Task<ApiResponse<BuildsRecord>> GetSuiteBuilds(
        [AliasAs("slug")] string weaponSlug,
        [AliasAs("f")] string? filterJsonStr = null,
        uint limit = 24,
        [AliasAs("_data")] string data = "features/builds/routes/builds.$slug"
    );
}
