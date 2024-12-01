using FluentScheduler;
using Jellyfish.Core.Data;
using Jellyfish.Module.Push.Weibo.Core;
using Jellyfish.Module.Push.Weibo.Data;
using Jellyfish.Util;
using Kook.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Module.Push.Weibo.Job;

/// <summary>
///     Scan user's weibo, push to channel when new weibo is sent.
/// </summary>
public class WeiboScanAndPushJob(
    BaseSocketClient kook,
    WeiboCrawlerService crawlerService,
    DbContextProvider dbProvider,
    ILogger<WeiboScanAndPushJob> log) : IAsyncJob
{
    public async Task ExecuteAsync()
    {
        log.LogInformation("开始扫描用户微博并准备推送");
        await using var dbCtx = dbProvider.Provide();
        foreach (var uid in dbCtx.WeiboPushConfigs
                     .AsNoTracking()
                     .GroupBy(c => c.Uid)
                     .Select(p => p.Key)
                     .ToArray())
        {
            log.LogInformation("开始准备扫描并推送微博用户{UID}", uid);
            var counts = await ScanAndPushAsync(dbCtx, uid);
            if (counts > 0) log.LogInformation("微博用户{UID}已推送完毕，共{Count}条", uid, counts);
            else log.LogInformation("未发现新微博，跳过微博用户{UID}", uid);
        }

        log.LogInformation("微博推送任务完成");
    }

    private async Task<int> ScanAndPushAsync(DatabaseContext dbCtx, string uid)
    {
        var weiboList = await crawlerService.CrawlAsync(uid);
        if (weiboList.IsEmpty()) return 0;

        // Check if all new weibo crawled
        var md5 = weiboList.Select(w => w.Md5).ToArray();
        var existed = dbCtx.WeiboCrawlHistories.AsNoTracking()
            .Where(it => md5.Contains(it.Hash))
            .ToList();

        var newWeiboList = weiboList.Where(it => existed.All(e => e.Hash != it.Md5)).ToList();
        // No new weibo found
        if (newWeiboList.IsEmpty()) return 0;

        // Save to crawl history
        var histories = newWeiboList
            .Select(w => new WeiboCrawlHistory(uid, w.Md5, w.Username, w.Content, w.Images.StringJoin(",")))
            .ToList();
        dbCtx.WeiboCrawlHistories.AddRange(histories);
        dbCtx.SaveChanges();

        var instances = (
            from i in dbCtx.WeiboPushInstances
                .Include(i => i.Config)
                .AsNoTracking()
            where i.Config.Uid == uid
            select i
        ).ToList();

        await PushToEachChannelAsync(dbCtx, instances, newWeiboList);
        return newWeiboList.Count;
    }

    private async Task PushToEachChannelAsync(DatabaseContext dbCtx, List<WeiboPushInstance> instances,
        List<WeiboItem> weiboList)
    {
        foreach (var instance in instances)
        {
            var channel = kook.GetGuild(instance.Config.GuildId)?.GetTextChannel(instance.ChannelId);
            if (channel == null) continue;

            foreach (var weibo in weiboList)
            {
                try
                {
                    await channel.SendCardSafeAsync(weibo.ToCard());
                    dbCtx.WeiboPushHistories.Add(new WeiboPushHistory(instance.Id, weibo.Md5));
                }
                catch (Exception e)
                {
                    log.LogError(e, "推送微博的过程中发生未知错误，用户：{Username}，内容：{Content}，MD5：{MD5}",
                        weibo.Username, weibo.Content[..(weibo.Content.Length > 20 ? 20 : weibo.Content.Length)],
                        weibo.Md5);
                }
            }

            dbCtx.SaveChanges();
        }
    }
}
