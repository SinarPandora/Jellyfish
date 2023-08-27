using Jellyfish.Core.Data;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace Jellyfish.Core.Cache;

/// <summary>
///     Global cache loader, init cache once before bot login
/// </summary>
public abstract class CacheLoader
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static async Task Load()
    {
        Log.Info("开始加载应用缓存");
        await LoadPermissions();
        Log.Info("应用缓存加载完成！");
    }

    /// <summary>
    ///     Load permissions
    /// </summary>
    public static async Task LoadPermissions()
    {
        await using var dbCtx = new DatabaseContext();
        var roles = await dbCtx.UserRoles
            .Include(e => e.CommandPermissions)
            .AsNoTracking()
            .ToListAsync();

        foreach (var role in roles)
        {
            foreach (var permission in role.CommandPermissions)
            {
                AppCaches.Permissions.AddOrUpdate($"{role.GuildId}_{permission.CommandName}",
                    new HashSet<uint> { role.KookId },
                    v =>
                    {
                        v.Add(role.KookId);
                        return v;
                    });
            }
        }
    }
}
