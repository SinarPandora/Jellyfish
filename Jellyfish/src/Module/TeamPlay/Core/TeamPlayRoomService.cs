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
    private const string RoomMemberLimitInvalid = "房间人数应 1~99 整数，或使用 0 代表不限人数";
    private const string UnsupportedPassword = "密码应为 1~12 位数字";

    #endregion

    private readonly KookSocketClient _kook;

    public TeamPlayRoomService(KookSocketClient kook)
    {
        _kook = kook;
    }

    /// <summary>
    ///     Create room instance, using text command
    /// </summary>
    /// <param name="args">Create room args</param>
    /// <param name="user">Current user</param>
    /// <param name="noticeChannel">Text channel for notice</param>
    /// <param name="onSuccess">Callback on success</param>
    public async Task CreateAndMoveToRoomAsync(
        Args.CreateRoomArgs args, SocketGuildUser user,
        IMessageChannel? noticeChannel,
        Func<TpRoomInstance, RestVoiceChannel, Task> onSuccess)
    {
        var tpConfig = args.Config;
        if (tpConfig.VoiceChannelId == null) return;

        var guild = _kook.GetGuild(tpConfig.GuildId);
        noticeChannel ??= await user.CreateDMChannelAsync();

        var roomName = tpConfig.RoomNamePattern != null
            ? tpConfig.RoomNamePattern.Replace(TeamPlayManageService.UserInjectKeyword,
                args.RoomName ?? user.DisplayName)
            : args.RoomName ?? $"{user.DisplayName}的房间";

        if (args.Password.IsNotEmpty())
        {
            if (args.Password.Length > 12 || !long.TryParse(args.Password, out _))
            {
                await noticeChannel.SendErrorCardAsync(UnsupportedPassword);
                return;
            }

            roomName = $"🔐{roomName}";
        }


        await using var dbCtx = new DatabaseContext();

        if (dbCtx.TpRoomInstances.Any(e => e.OwnerId == user.Id))
        {
            Log.Info($"创建频道 {roomName} 失败，用户 {user.DisplayName}#{user.Id} 已加入其他语音频道");
            await noticeChannel.SendErrorCardAsync(UserDoesNotFree);
            return;
        }

        var parentChannel = guild.GetVoiceChannel((ulong)tpConfig.VoiceChannelId);
        if (parentChannel == null)
        {
            Log.Error($"{tpConfig.Id}：{tpConfig.Name} 所对应的父频道未找到，请检查错误日志并更新频道配置");
            await noticeChannel.SendErrorCardAsync(ParentChannelNotFound);
            return;
        }

        int? memberLimit;
        if (args.RawMemberLimit != null)
        {
            if (!int.TryParse(args.RawMemberLimit, out var limit) || limit < 0 || limit > 99)
            {
                await noticeChannel.SendErrorCardAsync(RoomMemberLimitInvalid);
                return;
            }

            memberLimit = limit;
        }
        else
        {
            memberLimit = tpConfig.DefaultMemberLimit;
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

            if (args.Password.IsNotEmpty())
            {
                Log.Info($"检测到房间 {roomName} 带有初始密码，尝试设置密码");
                await room.ModifyAsync(v => v.Password = args.Password);
                Log.Info($"房间 {roomName} 密码设置成功！");
            }

            Log.Info($"创建语音房间 API 调用成功，房间名：{roomName}");

            Log.Info($"尝试移动用户所在房间，用户：{user.DisplayName()}，目标房间：{room.Name}");

            if (user.VoiceChannel != null)
            {
                await guild.MoveToRoomAsync(user, room);
            }

            Log.Info($"移动成功，用户已移动到{room.Name}");

            // Give owner permission
            await GiveOwnerPermissionAsync(room, user);

            var instance = new TpRoomInstance(
                tpConfigId: tpConfig.Id,
                voiceChannelId: room.Id,
                guildId: tpConfig.GuildId,
                roomName: roomName,
                ownerId: user.Id,
                memberLimit: memberLimit,
                commandText: args.RawCommand
            );
            dbCtx.TpRoomInstances.Add(instance);
            dbCtx.SaveChanges();

            Log.Info($"语音房间记录已保存：{roomName}");

            // Send post messages
            await SendRoomUpdateWizardToDmcAsync(
                tpConfig.TextChannelId == null
                    ? noticeChannel // Use the DMC created above
                    : await user.CreateDMChannelAsync() // Create new one
                , room.Name);
            await onSuccess(instance, room);
        }
        catch (Exception e)
        {
            Log.Error(e, "创建语音房间出错！");
            await noticeChannel.SendErrorCardAsync(ApiFailed);
        }
    }

    /// <summary>
    ///     Give voice channel permission to user
    /// </summary>
    /// <param name="channel">Room</param>
    /// <param name="user">Owner</param>
    public static async Task GiveOwnerPermissionAsync(IVoiceChannel channel, IGuildUser user)
    {
        await channel.AddPermissionOverwriteAsync(user);
        await channel.ModifyPermissionOverwriteAsync(user, permissions =>
            permissions.Modify(
                createInvites: PermValue.Allow,
                manageChannels: PermValue.Allow,
                manageVoice: PermValue.Allow,
                deafenMembers: PermValue.Allow,
                muteMembers: PermValue.Allow,
                playSoundtrack: PermValue.Allow,
                shareScreen: PermValue.Allow
            ));
    }

    /// <summary>
    ///     Create room invite card
    /// </summary>
    /// <param name="room">New voice channel</param>
    /// <returns>Kook card object</returns>
    public static async Task<Card> CreateInviteCardAsync(RestVoiceChannel room)
    {
        var invite = await room.CreateInviteAsync(InviteMaxAge.NeverExpires);
        var card = new CardBuilder();
        card.AddModule<HeaderModuleBuilder>(m => m.Text = $"✅房间已创建：{room.Name}，等你加入！");
        card.AddModule<InviteModuleBuilder>(m => m.Code = invite.Code);
        card.WithSize(CardSize.Large);
        return card.Build();
    }

    /// <summary>
    ///     Send room update wizard to DMC
    /// </summary>
    /// <param name="dmc">The DMC</param>
    /// <param name="roomName">Room name</param>
    public static async Task SendRoomUpdateWizardToDmcAsync(IMessageChannel dmc, string roomName)
    {
        await dmc.SendSuccessCardAsync(
            $"""
             您已成为房间 {roomName} 的房主
             作为房主，您可以随意修改房间信息，设置密码，调整麦序，全体静音等
             ---
             当所有人退出房间后，房间将被解散。
             """);
    }
}
