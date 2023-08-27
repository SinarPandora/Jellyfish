using FluentScheduler;
using Jellyfish.Core.Cache;
using Jellyfish.Core.Data;
using JetBrains.Annotations;
using Kook.WebSocket;
using Ninject;
using NLog;
using AppContext = Jellyfish.Core.Container.AppContext;

namespace Jellyfish.Module.Role.Job;

/// <summary>
///     Refresh role cache job, refresh cache for each guild at everyday 04:00
/// </summary>
[UsedImplicitly]
public class RefreshRoleCacheJob : IAsyncJob
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public async Task ExecuteAsync()
    {
        Log.Info("开始刷新工会权限信息");
        var client = AppContext.Instance.Get<KookSocketClient>();

        Log.Info("加载工会权限缓存");
        var guildRoleInfos = (from guild in client.Guilds
                select (guild.Id, guild.Roles.Select(r => new { r.Id, r.Name }).ToArray()))
            .ToDictionary(
                t => t.Id,
                t => t.Item2.ToDictionary(r => r.Id, r => r.Name)
            );
        Log.Info("工会权限缓存加载完成");

        await using var dbCtx = new DatabaseContext();

        Log.Info("开始刷新系统权限信息");
        dbCtx.UserRoles.GroupBy(e => e.GuildId).ToArray().ForEach(roleGroups =>
        {
            var cacheRoles = guildRoleInfos[roleGroups.Key];
            roleGroups.ForEach(role => { role.Name = cacheRoles[role.KookId]; });
        });

        await dbCtx.SaveChangesAsync();
        Log.Info("系统权限信息更新完毕");

        AppCaches.Permissions.Clear();
        await CacheLoader.LoadPermissions();
        Log.Info("工会权限信息刷新完成");
    }
}
