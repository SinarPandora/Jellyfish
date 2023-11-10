using Jellyfish.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Core.Cache;

/// <summary>
///     Global cache loader, init cache once before bot login
/// </summary>
public class CacheLoader
{
    private readonly ILogger<CacheLoader> _log;
    private readonly DbContextProvider _dbProvider;

    public CacheLoader(ILogger<CacheLoader> log, DbContextProvider dbProvider)
    {
        _log = log;
        _dbProvider = dbProvider;
    }

    public async Task Load()
    {
        await using var dbCtx = _dbProvider.Provide();
        _log.LogInformation("开始加载应用缓存");
        await LoadPermissions(dbCtx);
        LoadTeamPlayConfigs(dbCtx);
        _log.LogInformation("应用缓存加载完成！");
    }

    /// <summary>
    ///     Load permissions
    /// </summary>
    /// <param name="dbCtx">Database Context</param>
    private static async Task LoadPermissions(DatabaseContext dbCtx)
    {
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
    /// <param name="dbCtx">Database Context</param>
    private static void LoadTeamPlayConfigs(DatabaseContext dbCtx)
    {
        dbCtx.TpConfigs
            .Where(e => e.Enabled)
            .AsNoTracking()
            .AsEnumerable()
            .ForEach(c => AppCaches.TeamPlayConfigs.AddOrUpdate($"{c.GuildId}_{c.Name}", c));
    }
}
