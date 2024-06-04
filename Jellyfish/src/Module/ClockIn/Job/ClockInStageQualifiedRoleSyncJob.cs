using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FluentScheduler;
using Jellyfish.Core.Data;
using Jellyfish.Module.ClockIn.Data;
using Kook.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Module.ClockIn.Job;

/// <summary>
///     Job for syncing clock-in stage qualified roles
///     When the qualified role is modified of a stage, add a new role to the qualified user
/// </summary>
public class ClockInStageQualifiedRoleSyncJob : IAsyncJob
{
    // Buffer queues to avoid frequent calls to the Kook API
    private readonly Subject<(ClockInStageQualifiedHistory, SocketGuild, ClockInStage)> _buffer = new();
    private readonly DbContextProvider _dbProvider;
    private readonly KookSocketClient _kook;
    private readonly ILogger<ClockInStageQualifiedRoleSyncJob> _log;

    public ClockInStageQualifiedRoleSyncJob(DbContextProvider dbProvider,
        KookSocketClient kook,
        ILogger<ClockInStageQualifiedRoleSyncJob> log)
    {
        _dbProvider = dbProvider;
        _kook = kook;
        _log = log;

        _buffer
            .Buffer(TimeSpan.FromSeconds(1), 5, Scheduler.Default)
            .Where(list => list.Any())
            .Subscribe(group =>
                _ = UpdateQualifiedRole(
                    group.Select(g => g.Item1).ToArray(),
                    group.First().Item2,
                    group.First().Item3
                )
            );
    }

    public async Task ExecuteAsync()
    {
        try
        {
            _log.LogInformation("打卡阶段合格身份同步任务开始");
            await using var dbCtx = _dbProvider.Provide();
            var scope = dbCtx.ClockInStages.Include(s => s.Config)
                // Scan multiple times after stage updated in half an hour
                .Where(s => s.QualifiedRoleId != null && s.UpdateTime > DateTime.Now.AddMinutes(-30))
                .GroupBy(s => s.Config.GuildId)
                .ToArray();

            foreach (var group in scope)
            {
                var guild = _kook.GetGuild(group.Key);
                if (guild is null) continue;

                foreach (var stage in group)
                {
                    var histories = dbCtx.ClockInStageQualifiedHistories
                        .Include(h => h.UserStatus)
                        .AsNoTracking()
                        .Where(h => h.StageId == stage.Id
                                    && h.CreateTime < stage.UpdateTime
                                    && h.GivenRoleId != stage.QualifiedRoleId)
                        .ToArray();

                    if (histories.IsEmpty()) continue;
                    foreach (var history in histories)
                    {
                        _buffer.OnNext((history, guild, stage));
                    }
                }
            }

            _log.LogInformation("打卡阶段合格身份同步任务结束");
        }
        catch (Exception e)
        {
            _log.LogError(e, "打卡阶段合格身份同步任务失败");
        }
    }

    /// <summary>
    ///     Update the qualified role for users
    /// </summary>
    /// <param name="histories">Some history (a buffer)</param>
    /// <param name="guild">Current guild</param>
    /// <param name="stage">Current stage</param>
    private async Task UpdateQualifiedRole(ClockInStageQualifiedHistory[] histories, SocketGuild guild,
        ClockInStage stage)
    {
        await using var dbCtx = _dbProvider.Provide();
        foreach (var history in histories)
        {
            var kookUser = guild.GetUser(history.UserStatus.UserId);
            if (kookUser is null) return;
            if (!stage.QualifiedRoleId.HasValue &&
                kookUser.Roles.FirstOrDefault(r => r.Id == history.GivenRoleId) != null)
            {
                await kookUser.RemoveRoleAsync(history.GivenRoleId!.Value);
                _log.LogInformation("已删除旧用户合格角色，用户名：{Username}#{UserId}，阶段：{StageName}#{StageId}，服务器：{GuildName}",
                    kookUser.Username, kookUser.Id, stage.Name, stage.Id, guild.Name);
            }
            else if (stage.QualifiedRoleId.HasValue)
            {
                if (history.GivenRoleId.HasValue &&
                    kookUser.Roles.FirstOrDefault(r => r.Id == history.GivenRoleId) != null)
                {
                    await kookUser.RemoveRoleAsync(history.GivenRoleId!.Value);
                }

                if (kookUser.Roles.FirstOrDefault(r => r.Id == history.GivenRoleId) == null)
                {
                    var role = guild.GetRole(stage.QualifiedRoleId.Value);
                    if (role is not null)
                    {
                        await kookUser.AddRoleAsync(role.Id);
                        _log.LogInformation(
                            "用户合格角色已更新，用户名：{Username}#{UserId}，阶段：{StageName}#{StageId}，角色：{RoleName}#{RoleId}，服务器：{GuildName}",
                            kookUser.Username, kookUser.Id, stage.Name, stage.Id, role.Name, role.Id, guild.Name);
                    }
                }
            }

            history.GivenRoleId = stage.QualifiedRoleId;
            dbCtx.Update(history);
        }

        dbCtx.SaveChanges();
    }
}
