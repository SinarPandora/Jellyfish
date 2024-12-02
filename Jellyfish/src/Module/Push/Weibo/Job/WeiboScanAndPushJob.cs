using FluentScheduler;
using Jellyfish.Core.Data;
using Jellyfish.Module.Push.Weibo.Core;
using Jellyfish.Module.Push.Weibo.Data;
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

    private async Task<Dictionary<bool, List<WeiboItem>>> GetChangedWeiboAsync(DatabaseContext dbCtx, string uid)
    {
        var weiboList = await crawlerService.CrawlAsync(uid);
        if (weiboList.IsEmpty()) return [];

        Dictionary<bool, List<WeiboItem>> result = [];

        // Get new weibo
        var ids = weiboList.Select(w => w.Mid).ToArray();
        var existed = dbCtx.WeiboCrawlHistories
            .Where(it => ids.Contains(it.Mid))
            .ToList();

        var newWeiboList = weiboList.Where(it => existed.All(e => e.Mid != it.Mid)).ToList();
        if (newWeiboList.IsNotEmpty())
        {
            // Save to crawl history
            var histories = newWeiboList
                .Select(w => new WeiboCrawlHistory(uid, w.Md5, w.Username, w.Content, w.Images.StringJoin(","), w.Mid))
                .ToList();
            dbCtx.WeiboCrawlHistories.AddRange(histories);
            dbCtx.SaveChanges();
            result[true] = newWeiboList;
        }

        // Get changed weibo
        var changeWeiboList = weiboList.Where(it => existed.Exists(e => e.Mid == it.Mid && e.Hash != it.Md5)).ToList();
        if (!changeWeiboList.IsNotEmpty()) return result;

        foreach (var history in existed)
        {
            var weibo = changeWeiboList.FirstOrDefault(w => w.Mid == history.Mid);
            if (weibo is null) continue;
            history.Hash = weibo.Md5;
        }

        dbCtx.SaveChanges();
        result[true] = changeWeiboList;

        return result;
    }

    private async Task<int> ScanAndPushAsync(DatabaseContext dbCtx, string uid)
    {
        var changedWeibo = await GetChangedWeiboAsync(dbCtx, uid);

        var changeCount = 0;

        if (changedWeibo.TryGetValue(true, out var newWeiboList) && newWeiboList.IsNotNullOrEmpty())
        {
            var instances = (
                from i in dbCtx.WeiboPushInstances
                    .Include(i => i.Config)
                    .AsNoTracking()
                where i.Config.Uid == uid
                select i
            ).ToList();

            await PushToEachChannelAsync(dbCtx, instances, newWeiboList);
            changeCount += newWeiboList.Count;
        }

        if (!changedWeibo.TryGetValue(false, out var changedWeiboList) || !changedWeiboList.IsNotNullOrEmpty())
            return changeCount;

        await UpdatePrevWeiboAsync(dbCtx, changedWeiboList);
        changeCount += changedWeiboList.Count;

        return changeCount;
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
                    var message = await channel.SendCardAsync(weibo.ToCard());
                    dbCtx.WeiboPushHistories.Add(new WeiboPushHistory(instance.Id, weibo.Md5, weibo.Mid, message.Id));
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

    private async Task UpdatePrevWeiboAsync(DatabaseContext dbCtx, List<WeiboItem> weiboList)
    {
        foreach (var weibo in weiboList)
        {
            var histories = dbCtx.WeiboPushHistories
                .Include(h => h.Instance)
                .Include(h => h.Instance.Config)
                .Where(h => h.Mid == weibo.Mid && h.Hash != weibo.Md5)
                .ToList();

            foreach (var history in histories)
            {
                var channel = kook.GetGuild(history.Instance.Config.GuildId)
                    ?.GetTextChannel(history.Instance.ChannelId);
                if (channel == null) continue;
                try
                {
                    var message = await channel.GetMessageAsync(history.MessageId);
                    // API will return null when the message does not exist, actually.
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                    if (message is null)
                    {
                        // Resend
                        log.LogInformation("微博已被手动删除，不会重新发送，MID：{MID}", weibo.Mid);
                        continue;
                    }

                    // Update
                    await channel.ModifyMessageAsync(history.MessageId, x => x.Cards = [weibo.ToCard()]);
                    history.Hash = weibo.Md5;
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
