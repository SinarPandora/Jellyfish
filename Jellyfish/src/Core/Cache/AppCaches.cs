using CacheManager.Core;

namespace Jellyfish.Core.Cache;

/// <summary>
///     Application Caches
/// </summary>
public abstract class AppCaches
{
    /// <summary>
    ///     User Role Cache, GuildId as Key
    /// </summary>
    public static readonly ICacheManager<HashSet<string>> Permissions = CacheFactory.Build<HashSet<string>>(p =>
        p.WithDictionaryHandle());
}
