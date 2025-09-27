using Jellyfish.Core.Cache;
using Jellyfish.Core.Data;
using Jellyfish.Module.ClockIn.Data;
using Jellyfish.Util;
using Kook;
using Kook.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Module.ClockIn.Core;

/// <summary>
///     Clock-in management service
/// </summary>
public class ClockInManageService(DbContextProvider dbProvider)
{
    private const int MaxTopUserCountEachRank = 20;
    private const string TimeSuffixFormat = "HHmmss";

    /// <summary>
    ///     Enable clock-in for the guild
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <returns>Is task success</returns>
    public async Task<bool> Enable(SocketTextChannel channel)
    {
        await using var dbCtx = dbProvider.Provide();
        var config = await dbCtx.ClockInConfigs.FirstOrDefaultAsync(c => c.GuildId == channel.Guild.Id);
        if (config is null)
        {
            config = new ClockInConfig(channel.Guild.Id);
            dbCtx.ClockInConfigs.Add(config);
        }
        else if (config.Enabled)
        {
            await channel.SendWarningCardAsync("当前服务器已开启打卡功能", true);
            return false;
        }
        else
        {
            config.Enabled = true;
        }

        dbCtx.SaveChanges();
        AppCaches.ClockInConfigs.AddOrUpdate(channel.Guild.Id, config);
        await channel.SendSuccessCardAsync(
            """
            打卡功能已开启！
            ---
            您可以进一步配置打卡阶段。
            打卡阶段指的是连续/非连续的持续打卡次数，使用 `！打卡阶段` 指令配置。
            您可以利用该功能设置持续 N 天的打卡活动，例如：用户在本月内打卡 25 天即可满足条件参与抽奖。
            请使用 `！打卡阶段 帮助` 指令查看详细信息。
            """, false);
        return true;
    }

    /// <summary>
    ///     Disable clock-in for the guild
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <returns>Is task success</returns>
    public async Task<bool> Disable(SocketTextChannel channel)
    {
        await using var dbCtx = dbProvider.Provide();
        var config = await dbCtx.ClockInConfigs.FirstOrDefaultAsync(c => c.GuildId == channel.Guild.Id);
        if (config is null)
        {
            await channel.SendWarningCardAsync("当前服务器未开启打卡功能", true);
            return false;
        }

        if (!config.Enabled)
        {
            await channel.SendWarningCardAsync("当前服务器已关闭打卡功能", true);
            return false;
        }

        config.Enabled = false;

        dbCtx.SaveChanges();
        AppCaches.ClockInConfigs.Remove(channel.Guild.Id, out _);
        await channel.SendSuccessCardAsync("打卡功能已关闭", false);
        return true;
    }

    /// <summary>
    ///     Send clock-in card
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <returns>Is task success</returns>
    public async Task<bool> SendCard(SocketTextChannel channel)
    {
        await using var dbCtx = dbProvider.Provide();
        var config = await GetIfEnable(channel.Guild.Id, dbCtx);
        if (config is null)
        {
            await channel.SendWarningCardAsync("当前服务器未开启打卡功能，请先使用 `！打卡管理 启用` 开启", true);
            return false;
        }

        var exist = dbCtx.ClockInCardInstances
            .FirstOrDefault(e => e.ConfigId == config.Id && e.ChannelId == channel.Id);

        if (exist is not null)
        {
            await channel.SendWarningCardAsync("当前频道已存在打卡消息，若需重新发送，请先使用 `！打卡管理 撤回消息` 指令删除", true);
            return false;
        }

        var messageId = await SendCardToCurrentChannel(channel, config);
        var instance = new ClockInCardInstance(config.Id, channel.Id, messageId);
        dbCtx.ClockInCardInstances.Add(instance);
        dbCtx.SaveChanges();
        return false; // Return false to delete user message
    }


    /// <summary>
    ///     Recall clock-in card in the current channel
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <returns>Should keep the user message or not</returns>
    public async Task<bool> RecallCard(SocketTextChannel channel)
    {
        await using var dbCtx = dbProvider.Provide();
        var config = await GetIfEnable(channel.Guild.Id, dbCtx);
        if (config is null)
        {
            await channel.SendWarningCardAsync("当前服务器未开启打卡功能，请先使用 `！打卡管理 启用` 开启", true);
            return false;
        }

        var exist = dbCtx.ClockInCardInstances
            .FirstOrDefault(e => e.ConfigId == config.Id && e.ChannelId == channel.Id);

        if (exist is null)
        {
            await channel.SendWarningCardAsync("当前频道不存在打卡消息", true);
            return false;
        }

        await channel.DeleteMessageAsync(exist.MessageId);
        dbCtx.ClockInCardInstances.Remove(exist);
        dbCtx.SaveChanges();

        await channel.SendSuccessCardAsync("撤回成功！", true);
        return false; // Return false to delete user message
    }

    /// <summary>
    ///     Send clock-in card to the current channel
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="config">Clock-in config</param>
    /// <param name="appendData">Extra append data for clock-in card</param>
    /// <returns>Card message id</returns>
    public static async Task<Guid> SendCardToCurrentChannel(IMessageChannel channel, ClockInConfig config,
        ClockInCardAppendData? appendData = null)
    {
        var cardBuilder = new CardBuilder()
            .AddModule<HeaderModuleBuilder>(e => e.WithText(config.Title));

        if (config.Description.IsNotNullOrEmpty())
            cardBuilder.AddModule<SectionModuleBuilder>(e => e.WithText(config.Description, true));

        if (appendData is not null)
        {
            cardBuilder.AddModule<DividerModuleBuilder>()
                .AddModule<SectionModuleBuilder>(b =>
                    b.WithText($"""
                                今日已有{appendData.TodayClockInCount}人打卡
                                前{appendData.Top3Usernames.Length}名用户：
                                {appendData.Top3Usernames.StringJoin("\n")}
                                """));
        }

        cardBuilder.AddModule<DividerModuleBuilder>()
            .AddModule<ActionGroupModuleBuilder>(e => e.AddElement(b =>
            {
                b.WithText(config.ButtonText)
                    .WithClick(ButtonClickEventType.ReturnValue)
                    // Add time-based suffix to make sure the button is always new for the Kook server
                    .WithValue($"{ClockInCardAction.CardActionValue}_{DateTime.Now.ToString(TimeSuffixFormat)}")
                    .WithTheme(ButtonTheme.Primary);
            }));

        return (await channel.SendCardAsync(cardBuilder.Build())).Id;
    }

    /// <summary>
    ///     Common logic to update clock-in card config
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="update">Update action</param>
    /// <returns>Should keep the user message or not</returns>
    private async Task<bool> UpdateClockInCardConfig(SocketTextChannel channel, Action<ClockInConfig> update)
    {
        await using var dbCtx = dbProvider.Provide();
        var config = await GetIfEnable(channel.Guild.Id, dbCtx);
        if (config is null)
        {
            await channel.SendWarningCardAsync("当前服务器未开启打卡功能，请先使用 `！打卡管理 启用` 开启", true);
            return false;
        }

        update(config);
        dbCtx.SaveChanges();
        AppCaches.ClockInConfigs.AddOrUpdate(channel.Guild.Id, config);
        await channel.SendSuccessCardAsync("更新成功，卡片消息将在一分钟内刷新", false);
        return true;
    }

    /// <summary>
    ///     Set card title
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="title">Card new title</param>
    /// <returns>Is task success</returns>
    public Task<bool> SetCardTitle(SocketTextChannel channel, string title) =>
        UpdateClockInCardConfig(channel, config => config.Title = title);

    /// <summary>
    ///     Set card description
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="details">Card new description</param>
    /// <returns>Is task success</returns>
    public Task<bool> SetCardDescription(SocketTextChannel channel, string details) =>
        UpdateClockInCardConfig(channel, config => config.Description = details);

    /// <summary>
    ///     Set the card button name
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="name">Card button name</param>
    /// <returns>Is task success</returns>
    public Task<bool> SetCardButtonName(SocketTextChannel channel, string name) =>
        UpdateClockInCardConfig(channel, config => config.ButtonText = name);

    /// <summary>
    ///     List top N users, order by clock-in counts
    /// </summary>
    /// <param name="channel">Current channel</param>
    /// <param name="limit">Result limit</param>
    /// <returns>Is task success</returns>
    public async Task<bool> ListTopUsers(SocketTextChannel channel, string limit)
    {
        await using var dbCtx = dbProvider.Provide();
        var config = await GetIfEnable(channel.Guild.Id, dbCtx);
        if (config is null)
        {
            await channel.SendWarningCardAsync("当前服务器未开启打卡功能，请先使用 `！打卡管理 启用` 开启", true);
            return false;
        }

        if (!int.TryParse(limit, out var topCount) || topCount <= 0)
        {
            await channel.SendWarningCardAsync("参数错误，请输入大于 0 的整数", true);
            return false;
        }

        var topUsers = dbCtx.UserClockInStatuses
            .Where(e => e.ConfigId == config.Id)
            .GroupBy(e => e.AllClockInCount)
            .OrderByDescending(e => e.Key)
            .Take(topCount)
            .Select((g, i) => new
            {
                Rank = i + 1,
                Users = g.OrderBy(e => e.UpdateTime).Take(MaxTopUserCountEachRank + 1).ToList()
            })
            .Select(g => $"第 {
                g.Rank
            } 名：{
                g.Users.Select(u => $"{u.Username}#{u.IdNumber}").StringJoin("\n")
            }{
                (g.Users.Count > MaxTopUserCountEachRank ? "（按时间列出前 20 名）" : "")
            }")
            .AsEnumerable()
            .StringJoin("\n---\n");

        if (topUsers.IsEmpty())
        {
            await channel.SendInfoCardAsync("该服务器内还没有人打卡", false);
            return true;
        }

        await channel.SendSuccessCardAsync($"当前服务器打卡排行榜（前 {topCount} 名，包含并列）：\n---\n" + topUsers, false);
        return true;
    }

    /// <summary>
    ///     Get config if clock-in is enabled in this guild
    /// </summary>
    /// <param name="guildId">Current guild Id</param>
    /// <param name="dbCtx">Database context</param>
    /// <returns>Clock-in config if enable or else null</returns>
    public static async Task<ClockInConfig?> GetIfEnable(ulong guildId, DatabaseContext dbCtx)
    {
        var config = await dbCtx.ClockInConfigs.FirstOrDefaultAsync(c => c.GuildId == guildId);
        return config is null || !config.Enabled ? null : config;
    }
}
