using Jellyfish.Command.Role.Data;
using Jellyfish.Core.Cache;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace Jellyfish.Loader;

/// <summary>
///     Global cache loader, init cache once before bot login
/// </summary>
public abstract class CacheLoader
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static async Task Load()
    {
        Log.Info("开始加载应用缓存");
        await using var dbCtx = new DatabaseContext();
        var roles = await dbCtx.UserRoles
            .Include(e => e.CommandPermissions)
            .AsNoTracking()
            .ToListAsync();

        foreach (var role in roles)
        {
            foreach (var permission in role.CommandPermissions)
            {
                Caches.Roles.AddOrUpdate($"{role.GuildId}_{permission.CommandName}", new List<UserRole> { role },
                    v =>
                    {
                        v.Add(role);
                        return v;
                    });
            }
        }

        Log.Info("应用缓存加载完成！");
    }
}
