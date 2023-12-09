using Jellyfish.Core.Cache;
using Jellyfish.Core.Data;
using Kook.WebSocket;

namespace Jellyfish.Custom.GuildSetting.Core;

/// <summary>
///     Guild setting service
/// </summary>
public class GuildSettingService(
    ILogger<GuildSettingService> log,
    BaseSocketClient client,
    DbContextProvider dbProvider)
{
    /// <summary>
    ///     Init guild settings for all unset guild
    /// </summary>
    public async Task InitGuildSettings()
    {
        log.LogInformation("开始初始化默认配置");
        await using var dbCtx = dbProvider.Provide();
        var allSetGuild = dbCtx.GuildSettings.Select(e => e.GuildId).ToHashSet();
        var allGuild = client.Guilds.Select(g => g.Id).ToHashSet();
        foreach (var unsetGuildId in allGuild.Except(allSetGuild))
        {
            var setting = new Data.GuildSetting(unsetGuildId);
            AppCaches.GuildSettings.AddOrUpdate(unsetGuildId, setting.Setting);
            dbCtx.GuildSettings.Add(setting);
        }

        dbCtx.SaveChanges();
        log.LogInformation("配置初始化完成");
    }
}
