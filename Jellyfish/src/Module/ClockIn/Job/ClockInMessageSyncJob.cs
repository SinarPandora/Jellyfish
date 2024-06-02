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
            log.LogInformation("打开消息扫描任务开始");
            await using var dbCtx = dbProvider.Provide();
            var today = DateTime.Today;

            var needDelete = new List<ClockInCardInstance>();
            foreach (var group in dbCtx.ClockInCardInstances
                         .Include(i => i.Config)
                         .Include(i => i.Config.Histories.Where(h => h.CreateTime >= today))
                         .GroupBy(i => i.Config.GuildId))
            {
                var guild = kook.GetGuild(group.Key);
                if (guild is null)
                {
                    needDelete.AddRange(group);
                    continue;
                }

                foreach (var instance in group)
                {
                    var channel = guild.GetTextChannel(instance.ChannelId);
                    if (channel is null)
                    {
                        needDelete.Add(instance);
                        continue;
                    }

                    var lastMessage = await channel.GetMessagesAsync(limit: 1).FirstAsync();
                    if (lastMessage.IsEmpty())
                    {
                        instance.MessageId =
                            await ClockInManageService.SendCardToCurrentChannel(channel, instance.Config);
                    }
                    else if (lastMessage.First().Id != instance.MessageId ||
                             instance.Config.UpdateTime > instance.UpdateTime)
                    {
                        await channel.DeleteMessageAsync(instance.MessageId);
                        instance.MessageId =
                            await ClockInManageService.SendCardToCurrentChannel(channel, instance.Config);
                    }
                }
            }

            dbCtx.ClockInCardInstances.RemoveRange(needDelete);
            dbCtx.SaveChanges();

            log.LogInformation("打开消息扫描任务结束");
        }
        catch (Exception e)
        {
            log.LogError(e, "打卡消息扫描任务失败");
        }
    }
}
