using CacheManager.Core;

namespace Jellyfish.Core.Cache;

/// <summary>
///     Application Caches
/// </summary>
public abstract class AppCaches
{
    /// <summary>
    ///     User Role Cache, GuildId_CommandName as Key, a set of user role as value
    /// </summary>
    public static readonly ICacheManager<HashSet<uint>> Permissions = CacheFactory.Build<HashSet<uint>>(p =>
        p.WithDictionaryHandle());
}
