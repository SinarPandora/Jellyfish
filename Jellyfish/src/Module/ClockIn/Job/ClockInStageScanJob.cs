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
            var now = DateTime.Now;
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
                            && u.ClockInHistories.FirstOrDefault(h => h.CreateTime > stage.LastScanTime) != null
                        )
                        .ToArray();

                    await Task.WhenAll(userNeedScan.Select(u => ScanStageForUser(u, stage, guild)));
                    stage.LastScanTime = now;
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
            .Where(h =>
                h.UserStatusId == user.Id
                && h.CreateTime >= stage.StartDate.ToDateTime(TimeOnly.MinValue)
                && (stage.EndDate == null || h.CreateTime <= ((DateOnly)stage.EndDate).ToDateTime(TimeOnly.MaxValue))
            )
            .OrderBy(h => h.CreateTime)
            .ToArray();

        var window = new Queue<ClockInHistory>();
        var lastContinuousIndex = 0;
        var allowBreakDuration = stage.AllowBreakDays + 1;

        for (var i = 0; i < histories.Length; i++)
        {
            var history = histories[i];
            window.Enqueue(history);
            if (window.Count > WindowSize) window.Dequeue();
            if (window.Count != WindowSize) continue;
            if ((window.Last().CreateTime.Date - window.First().CreateTime.Date).Days > allowBreakDuration)
            {
                lastContinuousIndex = i;
            }
        }

        var qualifiedDays = histories.Length - lastContinuousIndex;
        if (qualifiedDays < stage.Days) return;

        // User is qualified
        dbCtx.ClockInStageQualifiedHistories.Add(
            new ClockInStageQualifiedHistory(stage.Id, user.Id, stage.QualifiedRoleId)
        );

        dbCtx.SaveChanges();
        log.LogInformation("用户已合格，用户名：{Username}#{UserId}，阶段：{StageName}#{StageId}，服务器：{GuildName}",
            user.Username, user.IdNumber, stage.Name, stage.Id, guild.Name);

        var guildUser = guild.GetUser(user.UserId);
        if (guildUser is null) return;

        if (stage.Config.ResultChannelId.HasValue)
        {
            var channel = guild.GetTextChannel(stage.Config.ResultChannelId.Value);
            if (channel is not null)
            {
                await channel.SendSuccessCardAsync(
                    $"{user.Username}#{user.IdNumber} 已合格！打卡阶段：{stage.Name}，累积打卡 {user.AllClockInCount} 天",
                    false
                );
            }
        }

        // Send the qualified message
        if (stage.QualifiedMessage.IsNotNullOrEmpty())
        {
            try
            {
                await guildUser.SendTextAsync(stage.QualifiedMessage!);
                log.LogInformation("用户合格，已发送合格消息，用户名：{Username}#{UserId}，阶段：{StageName}#{StageId}，服务器：{GuildName}",
                    user.Username, user.IdNumber, stage.Name, stage.Id, guild.Name);
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
            if (role is not null && guildUser.Roles.FirstOrDefault(r => r.Id == role.Id) == null)
            {
                await guildUser.AddRoleAsync(role);
                log.LogInformation("用户合格，已赋予指定角色，用户名：{Username}#{UserId}，阶段：{StageName}#{StageId}，服务器：{GuildName}",
                    user.Username, user.IdNumber, stage.Name, stage.Id, guild.Name);
            }
        }
    }
}
