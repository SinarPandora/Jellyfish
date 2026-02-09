using Jellyfish.Core.Data;
using Jellyfish.Core.Job;
using Jellyfish.Module.CountDownName.Core;
using Kook.WebSocket;

namespace Jellyfish.Module.CountDownName.Job;

/// <summary>
///     Channel countdown scan job
///     Update channel name based on the pattern when counting down
/// </summary>
public class CountDownScanJob(
    DbContextProvider dbProvider,
    KookSocketClient kook,
    ILogger<CountDownScanJob> log
) : IAsyncJob
{
    public async Task ExecuteAsync()
    {
        try
        {
            log.LogInformation("频道名称倒计时扫描任务开始");
            var today = DateTime.Today;
            await using var dbCtx = dbProvider.Provide();
            await foreach (var cdChannel in dbCtx.CountDownChannels)
            {
                var targetChannel = kook.GetGuild(cdChannel.GuildId)
                    ?.GetChannel(cdChannel.ChannelId);
                if (targetChannel is null)
                {
                    dbCtx.CountDownChannels.Remove(cdChannel);
                    continue;
                }

                var delta = (cdChannel.DueDate.ToDateTime(TimeOnly.MinValue) - today).Days;
                if (delta == 0 && !cdChannel.Positive)
                {
                    dbCtx.CountDownChannels.Remove(cdChannel);
                }

                await CountDownChannelService.UpdateChannelText(targetChannel, cdChannel);
            }

            dbCtx.SaveChanges();
            log.LogInformation("频道名称倒计时扫描任务结束");
        }
        catch (Exception e)
        {
            log.LogError(e, "频道名称倒计时扫描任务失败");
        }
    }
}
