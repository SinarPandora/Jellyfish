using Jellyfish.Core.Cache;
using Jellyfish.Core.Data;
using Jellyfish.Module.ClockIn.Data;
using Jellyfish.Util;
using Kook;
using Kook.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Module.ClockIn.Core;

/// <summary>
///     Clock-in stage management service
/// </summary>
public class ClockInStageManageService(DbContextProvider dbProvider)
{
    private const int MaxQualifiedUsersInMessage = 100;

    /// <summary>
    ///     List all stages in the current guild
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="type">The type of stage</param>
    /// <returns>Is task success or not</returns>
    public async Task<bool> List(SocketTextChannel channel, string type)
    {
        await using var dbCtx = dbProvider.Provide();
        var config = await ClockInManageService.GetIfEnable(channel.Guild.Id, dbCtx);
        if (config is null)
        {
            await channel.SendWarningCardAsync("当前服务器未开启打卡功能，请先使用 `！打卡 启用` 开启", true);
            return false;
        }

        var enabled = type == "启用";
        var stages = dbCtx.ClockInStages
            .Where(s => s.ConfigId == config.Id && s.Enabled == enabled)
            .OrderBy(s => s.Id)
            .Select(s => $"{s.Id}：{s.Name} {s.StartDate:yyyy-MM-dd} ~ {
                (s.EndDate.HasValue ? s.EndDate.Value.ToString("yyyy-MM-dd") : "至今")
            } 达标天数： {
                s.Days
            } 允许中断天数： {
                s.AllowBreakDays
            } 合格消息： {
                s.QualifiedMessage ?? "未设置"
            } 给予身份：{
                (s.QualifiedRoleId.HasValue && channel.Guild.GetRole(s.QualifiedRoleId.Value) != null
                    ? channel.Guild.GetRole(s.QualifiedRoleId.Value)!.Name
                    : "未设置"
                )
            }")
            .StringJoin("\n---\n");

        if (stages.IsEmpty())
        {
            await channel.SendSuccessCardAsync("当前服务器未设置打卡阶段", false);
            return true;
        }

        await channel.SendSuccessCardAsync($"{(enabled ? "启用的" : "禁用/过期的")}打卡阶段列表\n---\n" + stages, false);

        return true;
    }

    /// <summary>
    ///     Create a new stage
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="rawArgs">Arguments</param>
    /// <returns>Is task success or not</returns>
    public async Task<bool> Create(SocketTextChannel channel, string rawArgs)
    {
        var args = Regexs.MatchWhiteChars().Split(rawArgs, 3);
        if (args.Length < 3)
        {
            await channel.SendWarningCardAsync("创建打卡阶段需要提供名称、开始日期和达标天数，请查看帮助：`！打卡 帮助`", true);
            return false;
        }

        if (!DateOnly.TryParse(args[1], out var startDate))
        {
            await channel.SendWarningCardAsync("开始日期格式错误，请使用 `年-月-日` 格式，例如 `1997-07-29`", true);
            return false;
        }

        if (!uint.TryParse(args[2], out var days) || days <= 0)
        {
            await channel.SendWarningCardAsync("达标天数必须是大于 0 的整数", true);
            return false;
        }

        await using var dbCtx = dbProvider.Provide();
        var config = await ClockInManageService.GetIfEnable(channel.Guild.Id, dbCtx);
        if (config is null)
        {
            await channel.SendWarningCardAsync("当前服务器未开启打卡功能，请先使用 `！打卡 启用` 开启", true);
            return false;
        }

        var stage = new ClockInStage(config.Id, args.First(), startDate, days);
        dbCtx.ClockInStages.Add(stage);
        dbCtx.SaveChanges();

        AppCaches.ClockInConfigs[channel.Guild.Id].Stages.Add(stage);
        await channel.SendCardSafeAsync(
            new CardBuilder()
                .AddModule<HeaderModuleBuilder>(b => b.WithText($"打卡阶段：{args.First()} 创建成功！"))
                .AddModule<SectionModuleBuilder>(b =>
                    b.WithText($"""
                                当前配置信息如下：
                                状态：禁用中
                                开始日期：{startDate:yyyy-MM-dd}
                                结束日期：一直有效
                                达标天数：{days}
                                允许中断天数：0（必须一直连续打卡）
                                合格消息：未设置
                                给予身份：未设置
                                """, true)
                )
                .AddModule<DividerModuleBuilder>()
                .AddModule<SectionModuleBuilder>(b =>
                    b.WithText($"当一切都设置好后，请使用 `！打卡阶段 {stage.Id} 启用` 启用该打卡阶段", true)
                )
                .AddModule<DividerModuleBuilder>()
                .AddModule<SectionModuleBuilder>(b =>
                    b.WithText($"""
                                您可以使用以下指令修改阶段信息：
                                1. `！打卡阶段 {stage.Id} 开始日期 [日期（年-月-日）]`：修改开始日期
                                2. `！打卡阶段 {stage.Id} 结束日期 [日期（年-月-日）]`：修改结束日期（包含当天）
                                3. `！打卡阶段 {stage.Id} 达标天数 [大于 0 整数]`：修改达标天数
                                4. `！打卡阶段 {stage.Id} 合格消息 [消息内容]`：设置合格时向用户发送的消息
                                5. `！打卡阶段 {stage.Id} 给予身份 [@身份引用]`：设置合格时颁发给用户的 Kook 角色
                                6. `！打卡阶段 {stage.Id} 允许中断天数 [非负整数]`：修改允许中断天数，设置为 0 时不允许中断打卡
                                ---
                                当一切都设置好后，请使用 `！打卡阶段 {stage.Id} 启用` 启用该打卡阶段
                                ---
                                ！请注意：
                                打卡扫描任务持续在后台运行，若修改「合格消息」和「给予身份」的过程中有人满足条件则可能不会应用更改。
                                请务必在每次修改前禁用该阶段。
                                """, true))
                .Build());
        return true;
    }

    /// <summary>
    ///     Set the stage result channel
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="rawRef">Raw channel reference</param>
    /// <returns>Is task success or not</returns>
    public async Task<bool> SetResultChannel(SocketTextChannel channel, string rawRef)
    {
        if (!MentionUtils.TryParseChannel(rawRef, out var channelId, TagMode.KMarkdown))
        {
            await channel.SendWarningCardAsync("频道引用应是一个蓝色文本，请在消息框中输入#（井号）并在弹出的菜单中选择指定频道", true);
            return false;
        }

        await using var dbCtx = dbProvider.Provide();
        var config = await ClockInManageService.GetIfEnable(channel.Guild.Id, dbCtx);
        if (config is null)
        {
            await channel.SendWarningCardAsync("当前服务器未开启打卡功能，请先使用 `！打卡 启用` 开启", true);
            return false;
        }

        config.ResultChannelId = channelId;
        dbCtx.SaveChanges();
        AppCaches.ClockInConfigs.AddOrUpdate(channel.Guild.Id, config);

        await channel.SendSuccessCardAsync(
            $"""
             打卡结果频道已设置为 {MentionUtils.KMarkdownMentionChannel(channelId)}，请确保 Bot 有权限在指定频道发言。
             频道被删除后该功能自动取消。
             """, false);
        return true;
    }

    /// <summary>
    ///     Common logic for updating stage info
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="id">Stage id</param>
    /// <param name="update">Update logic</param>
    /// <returns>Is task success or not</returns>
    private async Task<bool> UpdateStage(SocketTextChannel channel, long id, Func<ClockInStage, Task<bool>> update)
    {
        await using var dbCtx = dbProvider.Provide();
        var stage = await GetIfExist(channel, id, dbCtx);
        if (stage is null) return false;

        var isSuccess = await update(stage);
        if (!isSuccess) return false;

        dbCtx.SaveChanges();
        AppCaches.ClockInConfigs[channel.Guild.Id].Stages.RemoveWhere(s => s.Id == stage.Id);
        AppCaches.ClockInConfigs[channel.Guild.Id].Stages.Add(stage);
        await channel.SendSuccessCardAsync("更新成功！", false);
        return true;
    }

    /// <summary>
    ///     Set the stage start date
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="id">Stage id</param>
    /// <param name="rawDate">Raw date</param>
    /// <returns>Is task success or not</returns>
    public Task<bool> SetStartDate(SocketTextChannel channel, long id, string rawDate) =>
        UpdateStage(channel, id, async stage =>
        {
            if (!DateOnly.TryParse(rawDate, out var date))
            {
                await channel.SendWarningCardAsync("日期格式错误，举例：`1997-7-29`", true);
                return false;
            }

            stage.StartDate = date;
            return true;
        });

    /// <summary>
    ///     Set the stage end date
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="id">Stage id</param>
    /// <param name="rawDate">Raw date</param>
    /// <returns>Is task success or not</returns>
    public Task<bool> SetEndDate(SocketTextChannel channel, long id, string rawDate) =>
        UpdateStage(channel, id, async stage =>
        {
            if (!DateOnly.TryParse(rawDate, out var date))
            {
                await channel.SendWarningCardAsync("日期格式错误，举例：`1997-7-29`", true);
                return false;
            }

            stage.EndDate = date;
            return true;
        });

    /// <summary>
    ///     Set the stage qualified counts
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="id">Stage id</param>
    /// <param name="rawCount">Raw count</param>
    /// <returns>Is task success or not</returns>
    public Task<bool> SetCount(SocketTextChannel channel, long id, string rawCount) =>
        UpdateStage(channel, id, async stage =>
        {
            if (!uint.TryParse(rawCount, out var days) || days == 0)
            {
                await channel.SendWarningCardAsync("达标日期格式错误，应是一个大于 0 的整数", true);
                return false;
            }

            stage.Days = days;
            return true;
        });

    /// <summary>
    ///     Set the stage qualified message
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="id">Stage id</param>
    /// <param name="message">Message text</param>
    /// <returns>Is task success or not</returns>
    public Task<bool> SetQualifiedMessage(SocketTextChannel channel, long id, string message) =>
        UpdateStage(channel, id, stage =>
        {
            stage.QualifiedMessage = message;
            return Task.FromResult(true);
        });

    /// <summary>
    ///     Set the stage allow break days
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="id">Stage id</param>
    /// <param name="rawCount">Raw count</param>
    /// <returns>Is task success or not</returns>
    public Task<bool> SetAllowBreakDays(SocketTextChannel channel, long id, string rawCount) =>
        UpdateStage(channel, id, async stage =>
        {
            if (!uint.TryParse(rawCount, out var days))
            {
                await channel.SendWarningCardAsync("允许中断天数格式错误，应是一个大于或等于 0 的整数", true);
                return false;
            }

            stage.AllowBreakDays = days;
            return true;
        });

    /// <summary>
    ///     Disable the stage
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="id">Stage id</param>
    /// <returns>Is task success or not</returns>
    public async Task<bool> Disable(SocketTextChannel channel, long id)
    {
        await using var dbCtx = dbProvider.Provide();
        var stage = await GetIfExist(channel, id, dbCtx);
        if (stage is null) return false;

        if (!stage.Enabled)
        {
            await channel.SendWarningCardAsync("当前阶段已经被禁用", true);
            return false;
        }

        stage.Enabled = false;
        dbCtx.SaveChanges();
        AppCaches.ClockInConfigs[channel.Guild.Id].Stages.RemoveWhere(s => s.Id == stage.Id);

        await channel.SendSuccessCardAsync($"已禁用打卡阶段：{stage.Name}#{stage.Id}，后续即使用户打卡记录达标，也不会被标记为满足该打卡阶段。", false);
        return true;
    }

    /// <summary>
    ///     Enable the stage
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="id">Stage id</param>
    /// <returns>Is task success or not</returns>
    public async Task<bool> Enable(SocketTextChannel channel, long id)
    {
        await using var dbCtx = dbProvider.Provide();
        var stage = await GetIfExist(channel, id, dbCtx);
        if (stage is null) return false;

        if (stage.Enabled)
        {
            await channel.SendInfoCardAsync("当前阶段正在启用中", true);
            return false;
        }

        stage.Enabled = true;
        dbCtx.SaveChanges();
        AppCaches.ClockInConfigs[channel.Guild.Id].Stages.Add(stage);

        await channel.SendSuccessCardAsync($"已重新启用该打卡阶段：{stage.Name}#{stage.Id}，明日零点过后满足条件的用户数据将被更新。", false);
        return true;
    }

    /// <summary>
    ///     List all qualified users in the stage (max 100)
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="id">Stage id</param>
    /// <returns>Is task success or not</returns>
    public async Task<bool> ListQualifiedUsers(SocketTextChannel channel, long id)
    {
        await using var dbCtx = dbProvider.Provide();
        var stage = await GetIfExist(channel, id, dbCtx);
        if (stage is null) return false;

        var qualifiedUsers = dbCtx.ClockInStageQualifiedHistories
            .Include(h => h.UserStatus)
            .Where(h => h.StageId == stage.Id)
            .OrderByDescending(h => h.UserStatus.AllClockInCount)
            .Take(MaxQualifiedUsersInMessage + 1)
            .ToArray();

        if (qualifiedUsers.IsEmpty())
        {
            await channel.SendInfoCardAsync("当前还没有用户达标", false);
            return true;
        }

        var text = qualifiedUsers
            .Select(h => $"{h.UserStatus.Username}#{h.UserStatus.UserId}（累计打卡{h.UserStatus.AllClockInCount}）")
            .StringJoin("\n");

        var cb = new CardBuilder()
            .AddModule<HeaderModuleBuilder>(b => b.WithText("满足条件的用户"))
            .AddModule<SectionModuleBuilder>(b => b.WithText(text, true));

        if (qualifiedUsers.Length > MaxQualifiedUsersInMessage)
        {
            cb
                .AddModule<DividerModuleBuilder>()
                .AddModule<SectionModuleBuilder>(b => b.WithText($"（仅列出前{MaxQualifiedUsersInMessage}名）"));
        }

        await channel.SendCardSafeAsync(cb.Build());
        return true;
    }

    /// <summary>
    ///     Get stage by id if exist
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="id">Stage id</param>
    /// <param name="dbCtx">Database context</param>
    /// <returns>Is task success or not</returns>
    private static async Task<ClockInStage?> GetIfExist(SocketTextChannel channel, long id, DatabaseContext dbCtx)
    {
        var config = await ClockInManageService.GetIfEnable(channel.Guild.Id, dbCtx);
        if (config is null)
        {
            await channel.SendWarningCardAsync("当前服务器未开启打卡功能，请先使用 `！打卡 启用` 开启", true);
            return null;
        }

        var stage = dbCtx.ClockInStages.FirstOrDefault(e => e.ConfigId == config.Id && e.Id == id);
        if (stage is not null) return stage;

        await channel.SendWarningCardAsync("该打卡阶段不存在，您可以使用 `！打卡阶段 列表 启用` 列出全部启用的打卡阶段", true);
        return null;
    }
}
