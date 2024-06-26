using FluentScheduler;
using Jellyfish.Core.Data;
using Jellyfish.Module.ExpireExtendSession.Data;
using Jellyfish.Module.TmpChannel.Data;
using Kook.WebSocket;

namespace Jellyfish.Module.TmpChannel.Job;

/// <summary>
///     Clean the text channel which does not exist in guild
/// </summary>
public class CleanNonExistTmpTextChannelJob(
    ILogger<CleanNonExistTmpTextChannelJob> log,
    BaseSocketClient kook,
    DbContextProvider dbProvider)
    : IAsyncJob
{
    public async Task ExecuteAsync()
    {
        await using var dbCtx = dbProvider.Provide();
        foreach (var (guildId, instances) in dbCtx.TmpTextChannels.GroupBy(i => i.GuildId).ToDictionary(i => i.Key))
        {
            var guild = kook.GetGuild(guildId);

            foreach (var instance in instances)
            {
                var textChannel = guild?.GetTextChannel(instance.ChannelId);
                if (textChannel is null) CleanUpTmpTextChannel(instance, dbCtx);

                dbCtx.SaveChanges();
            }
        }
    }

    /// <summary>
    ///     Clean up tmp text channel instance
    /// </summary>
    /// <param name="instance">Temporary text channel instance</param>
    /// <param name="dbCtx">Database context</param>
    private void CleanUpTmpTextChannel(TmpTextChannel instance, DatabaseContext dbCtx)
    {
        try
        {
            log.LogInformation("检测到临时文字房间已不存在，开始清理数据库记录，房间名称：{Name}，实例 ID：{Id}", instance.Name, instance.Id);
            var sessions = dbCtx.ExpireExtendSessions
                .Where(session =>
                    session.TargetId == instance.Id
                    && session.TargetType == ExtendTargetType.TmpTextChannel
                )
                .ToArray();

            if (sessions.IsNotNullOrEmpty()) log.LogInformation("检测到存在配套的过期时间刷新任务，将在稍后由过期扫描任务清理");

            dbCtx.TmpTextChannels.Remove(instance);
        }
        catch (Exception e)
        {
            log.LogError(e, "清理数据库记录失败，请检查数据库是否正常");
        }
    }
}
