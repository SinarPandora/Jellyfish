using FluentScheduler;
using Jellyfish.Core.Data;
using Jellyfish.Module.Push.Weibo.Data;
using Kook.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Module.Push.Weibo.Job;

/// <summary>
///     Try to resend a push that was not caused by a network issue failed
/// </summary>
public class WeiboPushFallbackJob(
    BaseSocketClient kook,
    DbContextProvider dbProvider,
    ILogger<WeiboPushFallbackJob> log) : IAsyncJob
{
    public async Task ExecuteAsync()
    {
        log.LogInformation("开始重试推送失败的微博");
        await using var dbCtx = dbProvider.Provide();
        foreach (var uid in dbCtx.WeiboPushConfigs
                     .AsNoTracking()
                     .GroupBy(c => c.Uid)
                     .Select(p => p.Key)
                     .ToArray())
        {
            log.LogInformation("开始重试推送{UID}的微博", uid);
            await ScanAndRePushAsync(dbCtx, uid, DateTime.Now.AddMinutes(-12), DateTime.Now.AddMinutes(-2));
            log.LogInformation("{UID}的微博已重试推送完毕", uid);
        }

        log.LogInformation("重试推送完成，若在此期间依然出现报错，请检查数据库并上报 bug");
    }

    private async Task ScanAndRePushAsync(DatabaseContext dbCtx, string uid, DateTime after, DateTime before)
    {
        var crawlHistories = dbCtx.WeiboCrawlHistories.AsNoTracking()
            .Where(h => h.CreateTime > after && h.CreateTime < before && h.Uid == uid)
            .ToList();

        var ids = crawlHistories.Select(h => h.Mid).ToArray();
        var incompletePushes = dbCtx.WeiboPushInstances
            .Include(h => h.PushHistories)
            .Include(h => h.Config)
            .AsNoTracking()
            .Where(it =>
                it.Config.Uid == uid &&
                // Not all Mid, exist in push history
                !ids.All(m => it.PushHistories.Any(h => h.Mid == m)))
            .ToList();

        foreach (var instance in incompletePushes)
        {
            var newWeiboList = crawlHistories.Where(it => instance.PushHistories.All(h => h.Mid != it.Mid)).ToList();
            if (newWeiboList.IsEmpty()) continue;

            var channel = kook.GetGuild(instance.Config.GuildId)?.GetTextChannel(instance.ChannelId);
            if (channel is null) continue;

            foreach (var weibo in newWeiboList)
            {
                try
                {
                    var message = await channel.SendCardAsync(weibo.ToCard());
                    dbCtx.WeiboPushHistories.Add(new WeiboPushHistory(instance.Id, weibo.Hash, weibo.Mid, message.Id));
                }
                catch (Exception e)
                {
                    log.LogError(e, "推送微博的过程中发生未知错误，用户：{Username}，内容：{Content}，MD5：{MD5}",
                        weibo.Username, weibo.Content[..(weibo.Content.Length > 20 ? 20 : weibo.Content.Length)],
                        weibo.Hash);
                }
            }

            dbCtx.SaveChanges();
        }
    }
}
