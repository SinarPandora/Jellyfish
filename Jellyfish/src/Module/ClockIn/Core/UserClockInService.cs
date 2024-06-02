using Jellyfish.Core.Cache;
using Jellyfish.Core.Data;
using Jellyfish.Module.ClockIn.Data;
using Jellyfish.Util;
using Kook.WebSocket;

namespace Jellyfish.Module.ClockIn.Core;

/// <summary>
///     Logic for user clock-in
/// </summary>
public class UserClockInService(KookSocketClient kook, DbContextProvider dbProvider)
{
    /// <summary>
    ///     User clock-in
    /// </summary>
    /// <param name="guildId">Guild id</param>
    /// <param name="channelId">Channel id</param>
    /// <param name="userId">User id</param>
    public async Task ClockIn(ulong guildId, ulong channelId, ulong userId)
    {
        // Check Guild, Channel and User exist
        var guild = kook.GetGuild(guildId);
        var channel = guild?.GetTextChannel(channelId);
        var user = guild?.GetUser(userId);
        if (channel is null || user is null) return;

        await using var dbCtx = dbProvider.Provide();
        // Check clock-in enabled
        if (!AppCaches.ClockInConfigs.TryGetValue(guildId, out var config)) return;

        // Check user clocked-in
        var today = DateTime.Today;
        var exists = (
            from item in config.Histories
            where item.CreateTime >= today.AddDays(-1)
            select item
        ).ToArray();
        if (exists.FirstOrDefault(i => i.CreateTime >= today) is not null)
        {
            await channel.SendInfoCardAsync("您今天已经打卡成功👍🏻，请明天再来", false);
            return;
        }

        // Init user status
        var userStatus = (
            from item in dbCtx.UserClockInStatuses
            where item.ConfigId == config.Id && item.UserId == userId
            select item
        ).FirstOrDefault();
        if (userStatus is null)
        {
            userStatus = new UserClockInStatus(config.Id, userId, user.DisplayName());
            dbCtx.UserClockInStatuses.Add(userStatus);
            dbCtx.SaveChanges();
        }
        else if (exists.FirstOrDefault(i => i.CreateTime < today && i.CreateTime >= today.AddDays(-1)) is null)
        {
            userStatus.StartDate = DateOnly.FromDateTime(today);
        }

        // Update cache
        userStatus.AllClockInCount += 1;

        // Record history
        dbCtx.ClockInHistories.Add(new ClockInHistory(config.Id, userId, channelId));
        dbCtx.SaveChanges();

        var ongoingDays = (today - userStatus.StartDate.ToDateTime(TimeOnly.MinValue)).Days + 1;
        await channel.SendSuccessCardAsync($"打卡成功！您已连续打卡 {ongoingDays} 天", false);
    }
}
