using FluentScheduler;
using Jellyfish.Core.Data;
using Jellyfish.Module.ClockIn.Core;
using Jellyfish.Module.ClockIn.Data;
using Kook.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Module.ClockIn.Job;

/// <summary>
///     Job for syncing clock-in messages of each channel
/// </summary>
public class ClockInMessageSyncJob(
    DbContextProvider dbProvider,
    KookSocketClient kook,
    ILogger<ClockInMessageSyncJob> log) : IAsyncJob
{
    public async Task ExecuteAsync()
    {
        try
        {
            log.LogInformation("打卡消息扫描任务开始");
            await using var dbCtx = dbProvider.Provide();
            var today = DateTime.Today;
            var now = DateTime.Now;

            var scope = dbCtx.ClockInCardInstances
                .Include(i => i.Config)
                .Where(i => i.Config.Enabled)
                .GroupBy(i => i.Config.GuildId)
                .ToArray();

            var needDelete = new List<ClockInCardInstance>();
            foreach (var group in scope)
            {
                var guild = await kook.Rest.GetGuildAsync(group.Key);
                if (guild.IsNull())
                {
                    needDelete.AddRange(group);
                    continue;
                }

                ClockInCardAppendData? appendData = null;
                ClockInHistory? lastHistory = null;
                var todayClockInCount =
                    dbCtx.ClockInHistories.Count(h => h.ConfigId == group.First().ConfigId && h.CreateTime >= today);
                if (todayClockInCount > 0)
                {
                    var top3Usernames = dbCtx.ClockInHistories.Include(h => h.UserStatus)
                        .Where(h => h.ConfigId == group.First().ConfigId && h.CreateTime >= today)
                        .OrderBy(h => h.CreateTime)
                        .Take(3)
                        .Select(u => $"{u.UserStatus.Username}#{u.UserStatus.IdNumber}")
                        .ToArray();
                    appendData = new ClockInCardAppendData(todayClockInCount, top3Usernames);
                    lastHistory = dbCtx.ClockInHistories.Where(h => h.CreateTime >= today)
                        .OrderByDescending(h => h.CreateTime).First();
                }

                foreach (var instance in group)
                {
                    var channel = await guild.GetTextChannelAsync(instance.ChannelId);
                    if (channel is null)
                    {
                        needDelete.Add(instance);
                        continue;
                    }

                    // --------------------------------------- Check history -------------------------------------------
                    var needResend = false;
                    var resendReason = string.Empty;

                    // Check if the config updated
                    if (instance.Config.UpdateTime > instance.UpdateTime)
                    {
                        needResend = true;
                        resendReason = "打卡配置更新";
                    }
                    // Check if the last update time is yesterday
                    else if ((today - instance.UpdateTime.Date).Days >= 1)
                    {
                        needResend = true;
                        resendReason = "现在是新的一天，每日打卡排行榜需重置";
                    }
                    // Check if the last update time is 1 hour ago
                    else if ((now - instance.UpdateTime).Hours >= 1)
                    {
                        needResend = true;
                        resendReason = "超过一小时未更新打卡信息";
                    }
                    else if (lastHistory != null && lastHistory.CreateTime > instance.UpdateTime)
                    {
                        needResend = true;
                        resendReason = "有新用户打卡";
                    }

                    if (needResend)
                    {
                        log.LogInformation("{Reason}，重新发送消息，频道：{ChannelName}#{ChannelId}，服务器：{GuildName}",
                            resendReason, channel.Name, channel.Id, guild.Name);
                        await channel.DeleteMessageAsync(instance.MessageId);
                        instance.MessageId =
                            await ClockInManageService.SendCardToCurrentChannel(channel, instance.Config, appendData);
                        continue;
                    }

                    // --------------------------------------- Check last message --------------------------------------
                    var lastMessage = await channel.GetMessagesAsync(1).FirstAsync();
                    // Check if the message deleted
                    if (lastMessage.IsEmpty())
                    {
                        instance.MessageId =
                            await ClockInManageService.SendCardToCurrentChannel(channel, instance.Config, appendData);
                        log.LogInformation("打卡消息被删除，重新发送消息，频道：{ChannelName}#{ChannelId}，服务器：{GuildName}",
                            channel.Name, channel.Id, guild.Name);
                    }
                    // Check if the message is not at the end
                    else if (lastMessage.First().Id != instance.MessageId)
                    {
                        log.LogInformation("打卡消息不在频道最底部，重新发送消息，频道：{ChannelName}#{ChannelId}，服务器：{GuildName}",
                            channel.Name, channel.Id, guild.Name);
                        await channel.DeleteMessageAsync(instance.MessageId);
                        instance.MessageId =
                            await ClockInManageService.SendCardToCurrentChannel(channel, instance.Config, appendData);
                    }
                }
            }

            dbCtx.ClockInCardInstances.RemoveRange(needDelete);
            dbCtx.SaveChanges();

            log.LogInformation("打卡消息扫描任务结束");
        }
        catch (Exception e)
        {
            log.LogError(e, "打卡消息扫描任务失败");
        }
    }
}
