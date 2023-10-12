using Jellyfish.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Core.Cache;

/// <summary>
///     Global cache loader, init cache once before bot login
/// </summary>
public class CacheLoader
{
    private readonly ILogger<CacheLoader> _log;
    private readonly DatabaseContext _dbCtx;

    public CacheLoader(ILogger<CacheLoader> log, DatabaseContext dbCtx)
    {
        _log = log;
        _dbCtx = dbCtx;
    }

    public async Task Load()
    {
        _log.LogInformation("开始加载应用缓存");
        await LoadPermissions();
        LoadTeamPlayConfigs();
        _log.LogInformation("应用缓存加载完成！");
    }

    /// <summary>
    ///     Load permissions
    /// </summary>
    private async Task LoadPermissions()
    {
        var roles = await _dbCtx.UserRoles
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
    private void LoadTeamPlayConfigs()
    {
        _dbCtx.TpConfigs
            .Where(e => e.Enabled)
            .AsNoTracking()
            .AsEnumerable()
            .ForEach(c => AppCaches.TeamPlayConfigs.AddOrUpdate($"{c.GuildId}_{c.Name}", c));
    }
}
