using CacheManager.Core;
using Jellyfish.Module.TeamPlay.Data;

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

    /// <summary>
    ///     Team Play Configuration Cache, GuildId_ConfigName as key, config object(without tracking) as value
    /// </summary>
    public static readonly ICacheManager<TpConfig> TeamPlayConfigs = CacheFactory.Build<TpConfig>(p =>
        p.WithDictionaryHandle());
}
