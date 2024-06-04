using FluentScheduler;
using Jellyfish.Core.Data;
using Jellyfish.Module.ClockIn.Data;
using Kook.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Module.ClockIn.Job;

/// <summary>
///     Clock-in stage scan job
///     Scan all users who clocked-in today to check if they meet any of the stage requirements
/// </summary>
public class ClockInStageScanJob(DbContextProvider dbProvider, KookSocketClient kook, ILogger<ClockInStageScanJob> log)
    : IAsyncJob
{
    private const int WindowSize = 2;

    public async Task ExecuteAsync()
    {
        try
        {
            log.LogInformation("打卡阶段合格状态扫描任务开始");
            await using var dbCtx = dbProvider.Provide();
            var today = DateTime.Today;
            var todayDate = DateOnly.FromDateTime(today);
            var window = new Queue<ClockInHistory>();

            foreach (var group in dbCtx.ClockInStages
                         .Include(s => s.Config)
                         .Include(s => s.QualifiedHistories)
                         .Include(s => s.Config.Histories.Where(h => h.CreateTime >= s.LastScanTime))
                         .Include(s => s.Config.UserStatuses.Where(u =>
                             s.QualifiedHistories.FirstOrDefault(h => h.UserStatusId == u.Id) == null
                             && s.Config.Histories.FirstOrDefault(h => h.UserStatusId == u.Id) != null
                         ))
                         .Where(s => s.Config.Enabled && s.Enabled && s.StartDate >= todayDate &&
                                     (s.EndDate == null || s.EndDate >= todayDate))
                         .GroupBy(s => s.Config.GuildId))
            {
                var guild = kook.GetGuild(group.Key);
                if (guild is null) continue;

                foreach (var stage in group)
                {
                    var allowBreakDuration = TimeSpan.FromDays(stage.AllowBreakDays + 1);
                    var lastContinuousIndex = 0;
                    window.Clear();
                    foreach (var userNeedScan in stage.Config.UserStatuses)
                    {
                        var histories = dbCtx.ClockInHistories
                            .Where(h => h.UserStatusId == userNeedScan.Id)
                            .OrderBy(h => h.CreateTime)
                            .ToArray();

                        for (var i = 0; i < histories.Length; i++)
                        {
                            var history = histories[i];
                            window.Enqueue(history);
                            if (window.Count > WindowSize)
                            {
                                window.Dequeue();
                            }

                            if (window.Count != WindowSize) continue;
                            if (window.Last().CreateTime.Date - window.First().CreateTime.Date > allowBreakDuration)
                            {
                                lastContinuousIndex = i;
                            }
                        }

                        var qualifiedDays = histories.Length - lastContinuousIndex;
                        if (qualifiedDays <= stage.Days) continue;

                        // User is qualified
                        dbCtx.ClockInStageQualifiedHistories.Add(
                            new ClockInStageQualifiedHistory(stage.Id, userNeedScan.Id)
                        );
                        dbCtx.SaveChanges();
                    }
                }
            }

            log.LogInformation("打卡阶段合格状态扫描任务结束");
        }
        catch (Exception e)
        {
            log.LogError(e, "打卡阶段合格状态扫描任务出错");
        }
    }
}
