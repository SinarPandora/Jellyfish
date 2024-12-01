using FluentScheduler;
using Jellyfish.Core.Data;
using Jellyfish.Module.Push.Weibo.Data;
using Jellyfish.Util;
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
            await ScanAndRePushAsync(dbCtx, uid, DateTime.Now.AddMinutes(-10));
        }

        log.LogInformation("重试推送完成，若在此期间依然出现报错，请检查数据库并上报 bug");
    }

    private async Task ScanAndRePushAsync(DatabaseContext dbCtx, string uid, DateTime period)
    {
        var histories = dbCtx.WeiboCrawlHistories.AsNoTracking()
            .Where(h => h.CreateTime > period && h.Uid == uid)
            .ToList();

        var md5 = histories.Select(h => h.Hash).ToArray();
        var existedGroups = dbCtx.WeiboPushHistories
            .Include(h => h.Instance)
            .Include(h => h.Instance.Config)
            .AsNoTracking()
            .Where(it => md5.Contains(it.Hash))
            .GroupBy(it => it.InstanceId)
            .ToList();

        foreach (var existed in existedGroups)
        {
            var newWeiboList = histories.Where(it => existed.All(h => h.Hash != it.Hash)).ToList();
            var instance = existed.FirstOrDefault()?.Instance;
            if (instance is null || newWeiboList.IsEmpty()) continue;

            var channel = kook.GetGuild(instance.Config.GuildId)?.GetTextChannel(instance.ChannelId);
            if (channel is null) continue;

            foreach (var weibo in newWeiboList)
            {
                try
                {
                    await channel.SendCardSafeAsync(weibo.ToCard());
                    dbCtx.WeiboPushHistories.Add(new WeiboPushHistory(instance.Id, weibo.Hash));
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
