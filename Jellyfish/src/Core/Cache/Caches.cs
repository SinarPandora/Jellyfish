using CacheManager.Core;
using Jellyfish.Command.Role.Data;

namespace Jellyfish.Core.Cache;

/// <summary>
///     Application Caches
/// </summary>
public abstract class Caches
{
    /// <summary>
    ///     User Role Cache, GuildId as Key
    /// </summary>
    public static readonly ICacheManager<List<UserRole>> Roles = CacheFactory.Build<List<UserRole>>(p =>
        p.WithDictionaryHandle());
}
