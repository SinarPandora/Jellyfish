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
    /// <returns>Should keep the user message or not</returns>
    public async Task<bool> List(SocketTextChannel channel, string type)
    {
        await using var dbCtx = dbProvider.Provide();
        var config = await ClockInManageService.GetIfEnable(channel.Guild.Id, dbCtx);
        if (config is null)
        {
            await channel.SendWarningCardAsync("当前服务器未开启打卡功能，请先使用 `！打卡管理 启用` 开启", true);
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
    /// <returns>Should keep the user message or not</returns>
    public async Task<bool> Create(SocketTextChannel channel, string rawArgs)
    {
        var args = Regexs.MatchWhiteChars().Split(rawArgs, 3);
        if (args.Length < 3)
        {
            await channel.SendWarningCardAsync("创建打卡阶段需要提供名称、开始日期和达标天数，请查看帮助：`！打卡管理 帮助`", true);
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
            await channel.SendWarningCardAsync("当前服务器未开启打卡功能，请先使用 `！打卡管理 启用` 开启", true);
            return false;
        }

        var stage = new ClockInStage(config.Id, args.First(), startDate, days);
        dbCtx.ClockInStages.Add(stage);
        dbCtx.SaveChanges();

        AppCaches.ClockInConfigs[channel.Guild.Id].Stages.Add(stage);
        await channel.SendSuccessCardAsync($"打卡阶段：{args.First()} 创建成功！", false);
        await SendInfoCard(channel, stage);
        return true;
    }

    /// <summary>
    ///     Show the stage information
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="id">Stage id</param>
    /// <returns>Is task success of not</returns>
    public async Task<bool> ShowInfo(SocketTextChannel channel, long id)
    {
        await using var dbCtx = dbProvider.Provide();
        var stage = await GetIfExist(channel, id, dbCtx);
        if (stage is null) return false;

        await SendInfoCard(channel, stage);
        return true;
    }

    /// <summary>
    ///     Send a card with information of the given stage
    /// </summary>
    /// <param name="channel">Target channel</param>
    /// <param name="stage">Current stage</param>
    /// <returns>Sent message id</returns>
    private static Task<Cacheable<IUserMessage, Guid>?> SendInfoCard(SocketTextChannel channel, ClockInStage stage)
    {
        var roleName = stage.QualifiedRoleId.HasValue
            ? channel.Guild.GetRole(stage.QualifiedRoleId.Value)?.Name ?? "权限丢失（若权限存在，您可以稍后重新执行该指令）"
            : "未设置";
        return channel.SendCardSafeAsync(
            new CardBuilder()
                .AddModule<HeaderModuleBuilder>(b => b.WithText($"打卡阶段 {stage.Name} 信息"))
                .AddModule<SectionModuleBuilder>(b =>
                    b.WithText($"""
                                当前配置信息如下：
                                编号：{stage.Id}
                                状态：{(stage.Enabled ? "启用中" : "禁用中")}
                                开始日期：{stage.StartDate:yyyy-MM-dd}
                                结束日期：{stage.EndDate?.ToString("yyyy-MM-dd") ?? "一直有效"}
                                达标天数：{stage.Days}
                                允许中断天数：{(stage.AllowBreakDays == 0 ? "0（必须一直连续打卡）" : stage.AllowBreakDays.ToString())}
                                合格消息：{stage.QualifiedMessage ?? "未设置"}
                                给予身份：{roleName}
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
                                使用该指令列出当前满足条件的用户（前 100 名，超过 100 名请使用「给予身份」功能，并在 Kook 的用户管理页面过滤查看）：
                                `！打卡阶段 {stage.Id} 满足条件的用户`
                                ---
                                ！请注意：
                                1. 打卡扫描任务持续在后台运行，修改过程中有人满足条件则不会应用新内容，请务必在每次修改前禁用该阶段。
                                2. 修改给予身份后，之前合格的用户将在五分钟内获得身份。
                                3. 修改合格消息后，之前合格的用户将不会再次发收到新消息。
                                """, true))
                .Build());
    }

    /// <summary>
    ///     Set the stage result channel
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="rawRef">Raw channel reference</param>
    /// <returns>Should keep the user message or not</returns>
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
            await channel.SendWarningCardAsync("当前服务器未开启打卡功能，请先使用 `！打卡管理 启用` 开启", true);
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
    /// <returns>Should keep the user message or not</returns>
    private async Task<bool> UpdateStage(SocketTextChannel channel, long id, Func<ClockInStage, Task<bool>> update)
    {
        await using var dbCtx = dbProvider.Provide();
        var stage = await GetIfExist(channel, id, dbCtx);
        if (stage is null) return false;

        var isSuccess = await update(stage);
        if (!isSuccess) return false;

        // Rescan all records after update
        stage.LastScanTime = DateTime.MinValue;

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
    /// <returns>Should keep the user message or not</returns>
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
    /// <returns>Should keep the user message or not</returns>
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
    /// <returns>Should keep the user message or not</returns>
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
    /// <returns>Should keep the user message or not</returns>
    public Task<bool> SetQualifiedMessage(SocketTextChannel channel, long id, string message) =>
        UpdateStage(channel, id, stage =>
        {
            stage.QualifiedMessage = message;
            return Task.FromResult(true);
        });

    /// <summary>
    ///     Set the stage qualified role
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="id">Stage id</param>
    /// <param name="roleRef">Role reference</param>
    /// <returns>Should keep the user message or not</returns>
    public Task<bool> SetQualifiedRole(SocketTextChannel channel, long id, string roleRef) =>
        UpdateStage(channel, id, async stage =>
        {
            if (!MentionUtils.TryParseRole(roleRef, out var roleId, TagMode.KMarkdown))
            {
                await channel.SendWarningCardAsync("角色引用应是一个蓝色文本，请在消息框中输入@（艾特符号）并在弹出的菜单中选择指定角色（不要选择用户）", true);
                return false;
            }

            stage.QualifiedRoleId = roleId;
            return true;
        });

    /// <summary>
    ///     Set the stage allow break days
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="id">Stage id</param>
    /// <param name="rawCount">Raw count</param>
    /// <returns>Should keep the user message or not</returns>
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
    /// <returns>Should keep the user message or not</returns>
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

        await channel.SendSuccessCardAsync($"已禁用打卡阶段：{stage.Name}#{stage.Id}，您可以开始修改打卡阶段配置，禁用的这段时间即使用户满足了条件也不会被标记为合格",
            false);
        return true;
    }

    /// <summary>
    ///     Enable the stage
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="id">Stage id</param>
    /// <returns>Should keep the user message or not</returns>
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

        await channel.SendSuccessCardAsync($"""
                                            已启用该打卡阶段：{stage.Name}#{stage.Id}，开始扫描合格用户
                                            为确保数据正确，下次修改前请依然先禁用该阶段
                                            """, false);
        return true;
    }

    /// <summary>
    ///     List all qualified users in the stage (max 100)
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="id">Stage id</param>
    /// <returns>Should keep the user message or not</returns>
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
            .Select(h => $"{h.UserStatus.Username}#{h.UserStatus.IdNumber}（累计打卡{h.UserStatus.AllClockInCount}）")
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
    /// <returns>Should keep the user message or not</returns>
    private static async Task<ClockInStage?> GetIfExist(SocketTextChannel channel, long id, DatabaseContext dbCtx)
    {
        var config = await ClockInManageService.GetIfEnable(channel.Guild.Id, dbCtx);
        if (config is null)
        {
            await channel.SendWarningCardAsync("当前服务器未开启打卡功能，请先使用 `！打卡管理 启用` 开启", true);
            return null;
        }

        var stage = dbCtx.ClockInStages.FirstOrDefault(e => e.ConfigId == config.Id && e.Id == id);
        if (stage is not null) return stage;

        await channel.SendWarningCardAsync("该打卡阶段不存在，您可以使用 `！打卡阶段 列表 启用` 列出全部启用的打卡阶段", true);
        return null;
    }
}
