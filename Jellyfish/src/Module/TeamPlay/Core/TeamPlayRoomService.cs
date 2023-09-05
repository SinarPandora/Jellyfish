using Jellyfish.Core.Data;
using Jellyfish.Module.TeamPlay.Data;
using Jellyfish.Util;
using Kook;
using Kook.Rest;
using Kook.WebSocket;
using NLog;

namespace Jellyfish.Module.TeamPlay.Core;

/// <summary>
///     Team play room service to handle room create or update actions
/// </summary>
public class TeamPlayRoomService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    #region ErrorMessage

    private const string ApiFailed = "操作失败，请稍后再试";
    private const string UserDoesNotFree = "您已加入到其他语音房间，请退出后再试";
    private const string ParentChannelNotFound = "父频道未找到，请联系频道管理员";
    private const string RoomMemberLimitInvalid = "房间人数应为正数，或使用 0 代表不限人数";
    private const string UserNotARoomOwner = "您还没有创建任何语音房间";
    private const string RoomNotFound = "房间未找到";

    #endregion


    /// <summary>
    ///     Create room instance, using text command
    /// </summary>
    /// <param name="args">Create room args</param>
    /// <param name="rawMsg">Raw user msg object</param>
    /// <param name="user">Current user</param>
    /// <param name="userChannel">Current channel</param>
    /// <param name="guild">Current guild</param>
    /// <param name="onSuccess">Callback on success</param>
    public static async Task CreateRoomWithCommand(
        Args.CreateRoomArgs args, SocketMessage rawMsg, SocketGuildUser user,
        IMessageChannel userChannel, SocketGuild guild,
        Func<(TpRoomInstance, RestVoiceChannel), Task> onSuccess)
    {
        if (args.Config.VoiceChannelId == null) return;
        var roomName = args.RoomName ?? $"{user.DisplayName}的房间";

        await using var dbCtx = new DatabaseContext();

        if (dbCtx.TpRoomInstances.Any(e => e.OwnerId == user.Id))
        {
            Log.Info($"创建频道 {roomName} 失败，用户 {user.DisplayName}#{user.Id} 已加入其他语音频道");
            await userChannel.SendErrorCardAsync(UserDoesNotFree);
            return;
        }

        var parentChannel = guild.GetVoiceChannel((ulong)args.Config.VoiceChannelId);
        if (parentChannel == null)
        {
            Log.Error($"{args.Config.Id}：{args.Config.Name} 所对应的父频道未找到，请检查错误日志并更新频道配置");
            await userChannel.SendErrorCardAsync(ParentChannelNotFound);
            return;
        }

        int? memberLimit;
        if (args.RawMemberLimit != null)
        {
            if (!int.TryParse(args.RawMemberLimit, out var limit) || limit < 0)
            {
                await userChannel.SendErrorCardAsync(RoomMemberLimitInvalid);
                return;
            }

            memberLimit = limit;
        }
        else
        {
            memberLimit = args.Config.DefaultMemberLimit;
        }

        memberLimit = memberLimit == 0 ? null : memberLimit + 1; // Add one more space for bot

        try
        {
            Log.Info($"开始创建语音房间{roomName}");
            var room = await guild.CreateVoiceChannelAsync(roomName, r =>
            {
                r.VoiceQuality = guild.GetHighestVoiceQuality();
                r.UserLimit = memberLimit;
                r.CategoryId = parentChannel.CategoryId;
            });
            Log.Info($"创建语音房间 API 调用成功，房间名：{roomName}");

            var instance = new TpRoomInstance(
                tpConfigId: args.Config.Id,
                voiceChannelId: room.Id,
                roomName: roomName,
                ownerId: user.Id,
                memberLimit: memberLimit,
                commandText: rawMsg.Content
            );
            dbCtx.TpRoomInstances.Add(instance);
            dbCtx.SaveChanges();

            Log.Info($"语音房间记录已保存：{roomName}");
            await onSuccess((instance, room));
        }
        catch (Exception e)
        {
            Log.Error(e, "创建语音房间出错！");
            await userChannel.SendErrorCardAsync(ApiFailed);
        }
    }

    /// <summary>
    ///     Update room member count
    ///     The new count is always 1 more than the user entered so that bot can join
    /// </summary>
    /// <param name="rawMemberLimit">New room member limit(raw input)</param>
    /// <param name="user">Current user</param>
    /// <param name="userChannel">Current channel</param>
    /// <param name="guild">Current guild</param>
    /// <param name="onSuccess">Callback on success</param>
    public static async Task UpdateRoomMemberLimit(
        string rawMemberLimit, SocketGuildUser user,
        IMessageChannel userChannel, SocketGuild guild,
        Func<Task> onSuccess)
    {
        if (!int.TryParse(rawMemberLimit, out var memberLimit) || memberLimit < 0)
        {
            Log.Info($"修改语音频道失败，{rawMemberLimit} 不是一个合法的房间人数值");
            await userChannel.SendErrorCardAsync(RoomMemberLimitInvalid);
            return;
        }

        await using var dbCtx = new DatabaseContext();
        var room = dbCtx.TpRoomInstances
            .FirstOrDefault(e => e.OwnerId == user.Id);

        if (room == null)
        {
            Log.Info($"修改语音频道失败，用户 {user.DisplayName}#{user.Id} 未创建任何房间");
            await userChannel.SendErrorCardAsync(UserNotARoomOwner);
            return;
        }

        var voiceChannel = guild.GetVoiceChannel(room.VoiceChannelId);
        if (voiceChannel == null)
        {
            Log.Info($"修改语音频道失败，房间 {room.VoiceChannelId} 不存在");
            await userChannel.SendErrorCardAsync(RoomNotFound);
            return;
        }

        room.MemberLimit = memberLimit == 0 ? null : memberLimit + 1;
        try
        {
            Log.Info($"开始修改语音房间 {room.RoomName} 人数到 {memberLimit}");
            await voiceChannel.ModifyAsync(v => { v.UserLimit = room.MemberLimit; });
            Log.Info($"修改房间 API 调用成功，房间名： {room.RoomName}");

            dbCtx.SaveChanges();
            Log.Info($"修改房间成功，房间名： {room.RoomName}，" +
                     $"房间人数：{(room.MemberLimit == 0 ? "无限制" : room.MemberLimit.ToString())}");
            await onSuccess();
        }
        catch (Exception e)
        {
            Log.Error(e, "修改语音房间人数出错！");
            await userChannel.SendErrorCardAsync(ApiFailed);
        }
    }

    /// <summary>
    ///     Update room name
    /// </summary>
    /// <param name="roomName">New room name</param>
    /// <param name="user">Current user</param>
    /// <param name="userChannel">Current channel</param>
    /// <param name="guild">Current guild</param>
    /// <param name="onSuccess">Callback on success</param>
    public static async Task UpdateRoomName(
        string roomName, SocketGuildUser user, IMessageChannel userChannel,
        SocketGuild guild, Func<Task> onSuccess)
    {
        await using var dbCtx = new DatabaseContext();
        var room = dbCtx.TpRoomInstances
            .FirstOrDefault(e => e.OwnerId == user.Id);

        if (room == null)
        {
            Log.Info($"修改房间名失败，用户 {user.DisplayName}#{user.Id} 未创建任何房间");
            await userChannel.SendErrorCardAsync(UserNotARoomOwner);
            return;
        }

        var voiceChannel = guild.GetVoiceChannel(room.VoiceChannelId);
        if (voiceChannel == null)
        {
            Log.Info($"修改语音频道失败，房间 {room.VoiceChannelId} 不存在");
            await userChannel.SendErrorCardAsync(RoomNotFound);
            return;
        }

        try
        {
            Log.Info($"开始修改语音房间 {room.RoomName} 名称为 {roomName}");
            await voiceChannel.ModifyAsync(v => { v.Name = roomName; });
            Log.Info($"修改房间 API 调用成功，房间名： {room.RoomName}");

            dbCtx.SaveChanges();
            Log.Info($"修改房间成功，当前房间名为： {room.RoomName}，");
            await onSuccess();
        }
        catch (Exception e)
        {
            Log.Error(e, "修改语音房间名出错！");
            await userChannel.SendErrorCardAsync(ApiFailed);
        }
    }

    /// <summary>
    ///     Dissolve room instance
    /// </summary>
    /// <param name="user">Current user</param>
    /// <param name="userChannel">Current channel</param>
    /// <param name="guild">Current guild</param>
    /// <param name="onSuccess">Callback on success</param>
    public static async Task DissolveRoomInstance(
        SocketGuildUser user, IMessageChannel userChannel,
        SocketGuild guild, Func<Task> onSuccess)
    {
        await using var dbCtx = new DatabaseContext();
        var room = dbCtx.TpRoomInstances
            .FirstOrDefault(e => e.OwnerId == user.Id);

        if (room == null)
        {
            Log.Info($"解散语音频道失败，用户 {user.DisplayName}#{user.Id} 未创建任何房间");
            await userChannel.SendErrorCardAsync(UserNotARoomOwner);
            return;
        }

        var voiceChannel = guild.GetVoiceChannel(room.VoiceChannelId);
        try
        {
            if (voiceChannel == null)
            {
                Log.Warn("解散语音频道失败，房间已解散，若该警告频繁发生，请优化这段代码");
            }
            else
            {
                Log.Info($"开始解散语音房间 {room.RoomName}");
                await voiceChannel.DeleteAsync();
                Log.Info($"删除语音房间 API 调用成功，房间：{room.Id}：{room.RoomName}");
            }

            dbCtx.TpRoomInstances.Remove(room);
            dbCtx.SaveChanges();
            Log.Info($"解散房间成功，房间：{room.Id}：{room.RoomName}");

            await onSuccess();
        }
        catch (Exception e)
        {
            Log.Error(e, "解散语音房间出错！");
            await userChannel.SendErrorCardAsync(ApiFailed);
        }
    }
}
