using FluentScheduler;
using Jellyfish.Core.Data;
using Jellyfish.Module.ClockIn.Data;
using Jellyfish.Util;
using Kook;
using Kook.Net;
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

            var scope = dbCtx.ClockInStages
                .Include(s => s.Config)
                .Where(s => s.Config.Enabled && s.Enabled && s.StartDate <= todayDate &&
                            (s.EndDate == null || s.EndDate >= todayDate))
                .GroupBy(s => s.Config.GuildId)
                .ToArray();

            foreach (var group in scope)
            {
                var guild = kook.GetGuild(group.Key);
                if (guild is null) continue;

                foreach (var stage in group)
                {
                    var userNeedScan = dbCtx.UserClockInStatuses
                        .Include(u => u.ClockInHistories)
                        .Include(u => u.QualifiedHistories)
                        .Where(u =>
                            // User has not been qualified
                            u.QualifiedHistories.FirstOrDefault(h => h.StageId == stage.Id) == null
                            // User has clocked after stage scanned
                            && u.ClockInHistories.FirstOrDefault(h => h.CreateTime.Date > stage.LastScanTime) != null
                        )
                        .ToArray();

                    await Task.WhenAll(userNeedScan.Select(u => ScanStageForUser(u, stage, guild)));

                    stage.LastScanTime = DateTime.Now;
                    dbCtx.SaveChanges();
                }
            }

            log.LogInformation("打卡阶段合格状态扫描任务结束");
        }
        catch (Exception e)
        {
            log.LogError(e, "打卡阶段合格状态扫描任务出错");
        }
    }

    /// <summary>
    ///     Scan if user is qualified for the stage
    /// </summary>
    /// <param name="user">User to scan</param>
    /// <param name="stage">Current stage</param>
    /// <param name="guild">Current guild</param>
    private async Task ScanStageForUser(UserClockInStatus user, ClockInStage stage, SocketGuild guild)
    {
        await using var dbCtx = dbProvider.Provide();
        var histories = dbCtx.ClockInHistories
            .Where(h => h.UserStatusId == user.Id)
            .OrderBy(h => h.CreateTime)
            .ToArray();

        var window = new Queue<ClockInHistory>();
        var lastContinuousIndex = 0;
        var allowBreakDuration = TimeSpan.FromDays(stage.AllowBreakDays + 1);

        for (var i = 0; i < histories.Length; i++)
        {
            var history = histories[i];
            window.Enqueue(history);
            if (window.Count > WindowSize) window.Dequeue();
            if (window.Count != WindowSize) continue;
            if (window.Last().CreateTime.Date - window.First().CreateTime.Date > allowBreakDuration)
            {
                lastContinuousIndex = i;
            }
        }

        var qualifiedDays = histories.Length - lastContinuousIndex;
        if (qualifiedDays <= stage.Days) return;

        // User is qualified
        dbCtx.ClockInStageQualifiedHistories.Add(
            new ClockInStageQualifiedHistory(stage.Id, user.Id)
        );

        // Send the qualified message
        if (stage.QualifiedMessage.IsNotNullOrEmpty())
        {
            try
            {
                kook.GetUser(user.UserId)?.SendTextAsync(stage.QualifiedMessage!);
            }
            catch (HttpException e)
            {
                if (e.Reason.IsNotNullOrEmpty() && e.Reason!.Contains(KookCoreApiHelper.HasBeenBlockedByUser))
                {
                    log.LogWarning("消息发送失败，Bot 已被对方屏蔽；该问题已被忽略，您可以从上下文中查找对应用户信息");
                }
            }
        }

        // Give the user the qualified role
        if (stage.QualifiedRoleId.HasValue)
        {
            var role = guild.GetRole(stage.QualifiedRoleId.Value);
            var kookUser = guild.GetUser(user.UserId);
            if (role is not null && kookUser is not null && kookUser.Roles.FirstOrDefault(r => r.Id == role.Id) == null)
            {
                await kookUser.AddRoleAsync(role);
            }
        }

        dbCtx.SaveChanges();
    }
}
