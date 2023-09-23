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
        await LoadTeamPlayConfigs();
        Log.Info("应用缓存加载完成！");
    }

    /// <summary>
    ///     Load permissions
    /// </summary>
    private static async Task LoadPermissions()
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
                    (_, v) =>
                    {
                        v.Add(role.KookId);
                        return v;
                    });
            }
        }
    }

    /// <summary>
    ///     Load team play configs
    /// </summary>
    private static async Task LoadTeamPlayConfigs()
    {
        await using var dbCtx = new DatabaseContext();
        dbCtx.TpConfigs
            .Where(e => e.Enabled)
            .AsNoTracking()
            .AsEnumerable()
            .ForEach(c => AppCaches.TeamPlayConfigs.AddOrUpdate($"{c.GuildId}_{c.Name}", c));
    }
}
