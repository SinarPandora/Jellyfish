using FluentScheduler;
using Jellyfish.Core.Data;
using Jellyfish.Module.TeamPlay.Core;
using Jellyfish.Module.TeamPlay.Data;
using Jellyfish.Util;
using Kook.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Module.TeamPlay.Job;

/// <summary>
///     Room scan job, scan all registered room's remove all empty
/// </summary>
public class TeamPlayRoomScanJob : IAsyncJob
{
    private readonly ILogger<TeamPlayRoomScanJob> _log;
    private readonly DatabaseContext _dbCtx;
    private readonly KookSocketClient _client;

    public TeamPlayRoomScanJob(KookSocketClient client, DatabaseContext dbCtx, ILogger<TeamPlayRoomScanJob> log)
    {
        _client = client;
        _dbCtx = dbCtx;
        _log = log;
    }

    /// <summary>
    ///     Entrypoint for AsyncJob
    /// </summary>
    public async Task ExecuteAsync()
    {
        var now = DateTime.Now;
        var configs = _dbCtx.TpConfigs.Include(e => e.RoomInstances)
            .GroupBy(e => e.GuildId)
            .ToDictionary(
                e => e.Key,
                e => e.SelectMany(c => c.RoomInstances)
            );

        foreach (var (guildId, rooms) in configs)
        {
            var guild = _client.GetGuild(guildId);
            foreach (var room in rooms)
            {
                // 2 minutes as timeout in order not to clean up room just created
                if (room.CreateTime.AddMinutes(2) >= now) continue;
                await CheckAndDeleteRoom(guild, room, _dbCtx);
                _dbCtx.SaveChanges(); // Save immediately for each room
            }
        }
    }

    /// <summary>
    ///     Check and delete room
    /// </summary>
    /// <param name="guild">Current guild</param>
    /// <param name="room">Room instance</param>
    /// <param name="_dbCtx">Database context</param>
    private async Task CheckAndDeleteRoom(SocketGuild guild, TpRoomInstance room, DatabaseContext _dbCtx)
    {
        var voiceChannel = guild.GetVoiceChannel(room.VoiceChannelId);
        try
        {
            // 1. Check if room not exist
            if (voiceChannel == null)
            {
                _dbCtx.TpRoomInstances.Remove(room);
                return;
            }

            // 2. Check if room empty
            var users = await voiceChannel.GetConnectedUsersAsync();
            if (users.All(u => u.IsBot ?? false))
            {
                _log.LogInformation("检测到房间 {RoomName} 只剩 bot 自己，开始清理房间", room.RoomName);
                await guild.DeleteVoiceChannelAsync(room.VoiceChannelId);
                _dbCtx.TpRoomInstances.Remove(room);
                _log.LogInformation("已删除房间：{RoomName}", room.RoomName);
            }

            // 3. Check if owner leave
            else if (users.Count > 0 && users.All(u => u.Id != room.OwnerId))
            {
                // If room owner not in the room, switch owner
                var newOwner =
                    (from user in users
                        where !(user.IsBot ?? false)
                        select user).FirstOrDefault();
                if (newOwner == null) return;

                _log.LogInformation("检测到房主离开房间 {RoomName}，将随机产生新房主", room.RoomName);
                await TeamPlayRoomService.GiveOwnerPermissionAsync(voiceChannel, newOwner);
                room.OwnerId = newOwner.Id;
                await TeamPlayRoomService.SendRoomUpdateWizardToDmcAsync(
                    await newOwner.CreateDMChannelAsync(),
                    room.RoomName
                );
                _log.LogInformation("新房主已产生，房间：{RoomName}，房主：{DisplayName}", room.RoomName, newOwner.DisplayName());
            }

            // 4. Check room name with 🔐locked icon if it has password(and also sync the name)
            var newRoomName = voiceChannel.Name;
            if (voiceChannel.HasPassword)
            {
                if (!newRoomName.StartsWith("🔐"))
                {
                    newRoomName = "🔐" + newRoomName;
                }
            }
            else if (newRoomName.StartsWith("🔐"))
            {
                newRoomName = newRoomName.ReplaceFirst("🔐", string.Empty);
            }

            if (newRoomName != voiceChannel.Name)
            {
                _log.LogInformation("监测到房间名称发生变化，尝试更新房间名");
                await voiceChannel.ModifyAsync(v => v.Name = newRoomName);
            }

            room.RoomName = newRoomName;
        }
        catch (Exception e)
        {
            _log.LogError(e, "尝试清理房间失败，房间名：{RoomName}", room.RoomName);
        }
    }
}
