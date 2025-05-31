using FluentScheduler;
using Jellyfish.Core.Data;
using Jellyfish.Module.RecallMessageMonitor.Data;
using Jellyfish.Util;
using Kook.Rest;
using Kook.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Module.RecallMessageMonitor.Job;

/// <summary>
///     Job to ensure the messages are eventually recalled.
/// </summary>
public class EnsureMessageRecalledJob(
    DbContextProvider dbProvider,
    KookSocketClient kook,
    ILogger<EnsureMessageRecalledJob> log) : IAsyncJob
{
    public async Task ExecuteAsync()
    {
        try
        {
            log.LogInformation("确保消息撤回任务开始");
            await using var dbCtx = dbProvider.Provide();
            var messageGroups = dbCtx.RecallMessages.AsNoTracking()
                .ToArray()
                .GroupBy(it => (it.GuildId, it.ChannelId));

            var needDelete = new List<RecallMessage>();
            foreach (var group in messageGroups)
            {
                var (guildId, channelId) = group.Key;
                var guild = (RestGuild?)await kook.Rest.GetGuildAsync(guildId);
                if (guild == null)
                {
                    needDelete.AddRange(group);
                    continue;
                }

                var channel = await guild.GetTextChannelAsync(channelId);
                if (channel == null)
                {
                    needDelete.AddRange(group);
                    continue;
                }

                foreach (var message in group)
                {
                    try
                    {
                        await channel.DeleteMessageSafeAsync(message.MessageId);
                        needDelete.Add(message);
                    }
                    catch (Exception e)
                    {
                        log.LogWarning(e, "删除消息时出错，房间名：{ChannelName}，消息 ID：{MessageMessageId}", channel.Name,
                            message.MessageId);
                    }
                }
            }

            dbCtx.RecallMessages.RemoveRange(dbCtx.RecallMessages.Where(it => needDelete.Contains(it)));
            dbCtx.SaveChanges();

            log.LogInformation("确保消息撤回任务结束，已清理：{NeedDeleteCount}条顽固消息", needDelete.Count);
        }
        catch (Exception e)
        {
            log.LogError(e, "确保消息撤回任务失败");
        }
    }
}
