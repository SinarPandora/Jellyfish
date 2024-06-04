using Jellyfish.Core.Cache;
using Jellyfish.Core.Data;
using Jellyfish.Module.ClockIn.Data;
using Jellyfish.Util;
using Kook;
using Kook.WebSocket;

namespace Jellyfish.Module.ClockIn.Core;

/// <summary>
///     Logic for user clock-in
/// </summary>
public class UserClockInService(KookSocketClient kook, DbContextProvider dbProvider, ILogger<UserClockInService> log)
{
    /// <summary>
    ///     User clock-in
    /// </summary>
    /// <param name="guildId">Guild id</param>
    /// <param name="channelId">Channel id</param>
    /// <param name="userId">User id</param>
    /// <param name="fromButton">Is clock-in from button or not</param>
    public async Task ClockIn(ulong guildId, ulong channelId, ulong userId, bool fromButton)
    {
        // Check Guild, Channel and User exist
        var guild = kook.GetGuild(guildId);
        var channel = guild?.GetTextChannel(channelId);
        var user = guild?.GetUser(userId);
        if (channel is null || user is null) return;

        await using var dbCtx = dbProvider.Provide();
        // Check clock-in enabled
        if (!AppCaches.ClockInConfigs.TryGetValue(guildId, out var config)) return;

        // Init user status
        var firstTimeClockIn = false;
        var userStatus = (
            from item in dbCtx.UserClockInStatuses
            where item.ConfigId == config.Id && item.UserId == userId
            select item
        ).FirstOrDefault();
        if (userStatus is null)
        {
            firstTimeClockIn = true;
            userStatus = new UserClockInStatus(config.Id, userId, user.DisplayName())
            {
                AllClockInCount = 1
            };
            dbCtx.UserClockInStatuses.Add(userStatus);
            dbCtx.SaveChanges();
        }

        // Check user clocked-in
        var today = DateTime.Today;
        if (!firstTimeClockIn)
        {
            var exists = (
                from item in dbCtx.ClockInHistories
                where item.UserStatusId == userStatus.Id && item.CreateTime >= today.AddDays(-1)
                select item
            ).ToArray();
            if (exists.FirstOrDefault(i => i.CreateTime >= today) is not null)
            {
                await channel.SendInfoCardAsync($"{MentionUtils.KMarkdownMentionUser(userId)} 您今天已经打卡成功👍🏻，请明天再来",
                    fromButton);
                return;
            }

            if (exists.FirstOrDefault(i => i.CreateTime < today && i.CreateTime >= today.AddDays(-1)) is null)
            {
                userStatus.StartDate = DateOnly.FromDateTime(today);
            }

            // Update cache
            userStatus.AllClockInCount += 1;
        }

        // Record history
        dbCtx.ClockInHistories.Add(new ClockInHistory(config.Id, userStatus.Id, channelId));
        dbCtx.SaveChanges();

        var ongoingDays = (today - userStatus.StartDate.ToDateTime(TimeOnly.MinValue)).Days + 1;
        await channel.SendSuccessCardAsync($"{MentionUtils.KMarkdownMentionUser(userId)} 打卡成功！您已连续打卡 {ongoingDays} 天",
            fromButton);
        log.LogInformation("用户打卡成功，用户名：{UserName}#{UserId}，服务器：{GuildName}",
            userStatus.Username, userStatus.UserId, guild!.Name);
    }
}
