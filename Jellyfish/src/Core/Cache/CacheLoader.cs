using Jellyfish.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Core.Cache;

/// <summary>
///     Global cache loader, init cache once before bot login
/// </summary>
public class CacheLoader(ILogger<CacheLoader> log, DbContextProvider dbProvider)
{
    public async Task Load()
    {
        await using var dbCtx = dbProvider.Provide();
        log.LogInformation("开始加载应用缓存");
        await LoadPermissions(dbCtx);
        LoadTeamPlayConfigs(dbCtx);
        LoadGuildSettings(dbCtx);
        LoadClockInConfig(dbCtx);
        log.LogInformation("应用缓存加载完成！");
    }

    /// <summary>
    ///     Load permissions
    /// </summary>
    /// <param name="dbCtx">Database Context</param>
    private static async Task LoadPermissions(DatabaseContext dbCtx)
    {
        var roles = await dbCtx
            .UserRoles.Include(e => e.CommandPermissions)
            .AsNoTracking()
            .ToListAsync();

        foreach (var role in roles)
        {
            foreach (var permission in role.CommandPermissions)
            {
                AppCaches.Permissions.AddOrUpdate(
                    $"{role.GuildId}_{permission.CommandName}",
                    [role.KookId],
                    (_, v) =>
                    {
                        v.Add(role.KookId);
                        return v;
                    }
                );
            }
        }
    }

    /// <summary>
    ///     Load team play configs
    /// </summary>
    /// <param name="dbCtx">Database Context</param>
    private static void LoadTeamPlayConfigs(DatabaseContext dbCtx)
    {
        dbCtx
            .TpConfigs.Where(e => e.Enabled)
            .AsNoTracking()
            .AsEnumerable()
            .ForEach(c => AppCaches.TeamPlayConfigs.AddOrUpdate($"{c.GuildId}_{c.Name}", c));
    }

    /// <summary>
    ///     Load guild settings
    /// </summary>
    /// <param name="dbCtx">Database context</param>
    private static void LoadGuildSettings(DatabaseContext dbCtx)
    {
        dbCtx
            .GuildSettings.AsNoTracking()
            .AsEnumerable()
            .ForEach(s => AppCaches.GuildSettings.AddOrUpdate(s.GuildId, s.Setting));
    }

    /// <summary>
    ///     Load clock-in configs
    /// </summary>
    /// <param name="dbCtx">Database context</param>
    private static void LoadClockInConfig(DatabaseContext dbCtx)
    {
        dbCtx
            .ClockInConfigs.Include(c => c.Stages)
            .AsNoTracking()
            .AsEnumerable()
            .ForEach(c => AppCaches.ClockInConfigs.AddOrUpdate(c.GuildId, c));
    }
}
