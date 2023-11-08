using Jellyfish.Core.Data;
using Jellyfish.Module.TeamPlay.Data;
using Jellyfish.Module.TmpChannel.Core;
using Jellyfish.Util;
using Kook;
using Kook.Rest;
using Kook.WebSocket;

namespace Jellyfish.Module.TeamPlay.Core;

/// <summary>
///     Team play room service to handle room create or update actions
/// </summary>
public class TeamPlayRoomService
{
    #region ErrorMessage

    private const string UserDoesNotFree = "您已加入到其他语音房间，请退出后再试";
    private const string ParentChannelNotFound = "父频道未找到，请联系频道管理员";
    private const string RoomMemberLimitInvalid = "房间人数应 1~99 整数，或使用 0 代表不限人数";
    private const string UnsupportedPassword = "密码应为 1~12 位数字";

    private const string FailToCreateTmpTextChannel = """
                                                      创建配套的临时文字房间失败，若您非常需要使用该功能，请退出当前组队语音房间，
                                                      等待您创建的房间被清理后重新创建一次。
                                                      若此问题重复出现，请联系请与相关工作人员。
                                                      """;

    #endregion

    private readonly DatabaseContext _dbCtx;
    private readonly ILogger<TeamPlayRoomService> _log;
    private readonly TmpTextChannelService _tmpTextChannelService;

    public TeamPlayRoomService(DatabaseContext dbCtx, ILogger<TeamPlayRoomService> log,
        TmpTextChannelService tmpTextChannelService)
    {
        _dbCtx = dbCtx;
        _log = log;
        _tmpTextChannelService = tmpTextChannelService;
    }

    /// <summary>
    ///     Create room instance, using text command
    /// </summary>
    /// <param name="args">Create room args</param>
    /// <param name="user">Current user</param>
    /// <param name="noticeChannel">Text channel for notice</param>
    /// <param name="onSuccess">Callback on success</param>
    /// <returns>Is task success</returns>
    public async Task<bool> CreateAndMoveToRoomAsync(
        Args.CreateRoomArgs args, SocketGuildUser user,
        IMessageChannel? noticeChannel,
        Func<TpRoomInstance, RestVoiceChannel, Task> onSuccess)
    {
        var tpConfig = args.Config;
        if (!tpConfig.VoiceChannelId.HasValue) return false;

        var guild = user.Guild;
        noticeChannel ??= await user.CreateDMChannelAsync();

        var roomName = tpConfig.RoomNamePattern != null
            ? tpConfig.RoomNamePattern.Replace(TeamPlayManageService.UserInjectKeyword,
                args.RoomName ?? user.DisplayName)
            : args.RoomName ?? $"{user.DisplayName}的房间";

        if (args.Password.IsNotEmpty())
        {
            if (args.Password.Length > 12 || !long.TryParse(args.Password, out _))
            {
                await noticeChannel.SendErrorCardAsync(UnsupportedPassword, true);
                return false;
            }

            roomName = $"🔐{roomName}";
        }

        if (_dbCtx.TpRoomInstances.Any(e => e.OwnerId == user.Id))
        {
            _log.LogInformation("创建频道 {RoomName} 失败，用户 {DisplayName}#{UserId} 已加入其他语音频道", roomName, user.DisplayName,
                user.Id);
            await noticeChannel.SendErrorCardAsync(UserDoesNotFree, true);
            return false;
        }

        var parentChannel = guild.GetVoiceChannel(tpConfig.VoiceChannelId.Value);
        if (parentChannel == null)
        {
            _log.LogError("{TpConfigId}：{TpConfigName} 所对应的父频道未找到，请检查错误日志并更新频道配置", tpConfig.Id, tpConfig.Name);
            await noticeChannel.SendErrorCardAsync(ParentChannelNotFound, true);
            return false;
        }

        int? memberLimit;
        if (args.RawMemberLimit != null)
        {
            if (!int.TryParse(args.RawMemberLimit, out var limit) || limit < 0 || limit > 99)
            {
                await noticeChannel.SendErrorCardAsync(RoomMemberLimitInvalid, true);
                return false;
            }

            memberLimit = limit;
        }
        else
        {
            memberLimit = tpConfig.DefaultMemberLimit;
        }

        memberLimit = memberLimit == 0 ? null : memberLimit; // Do not leave a member for bot

        try
        {
            _log.LogInformation("开始创建语音房间{RoomName}", roomName);
            var room = await guild.CreateVoiceChannelAsync(roomName, r =>
            {
                r.VoiceQuality = guild.GetHighestVoiceQuality();
                r.UserLimit = memberLimit;
                r.CategoryId = parentChannel.CategoryId;
            });

            if (args.Password.IsNotEmpty())
            {
                _log.LogInformation("检测到房间 {RoomName} 带有初始密码，尝试设置密码", roomName);
                await room.ModifyAsync(v => v.Password = args.Password);
                _log.LogInformation("房间 {RoomName} 密码设置成功！", roomName);
            }

            // Give owner permission
            await GiveOwnerPermissionAsync(room, user);

            _log.LogInformation("创建语音房间 API 调用成功，房间名：{RoomName}", roomName);

            _log.LogInformation("尝试移动用户所在房间，用户：{DisplayName}，目标房间：{RoomName}", user.DisplayName(), room.Name);

            if (user.VoiceChannel != null)
            {
                await guild.MoveToRoomAsync(user.Id, room);
            }

            _log.LogInformation("移动成功，用户已移动到{RoomName}", room.Name);

            var instance = new TpRoomInstance(
                tpConfigId: tpConfig.Id,
                voiceChannelId: room.Id,
                guildId: tpConfig.GuildId,
                roomName: roomName,
                ownerId: user.Id,
                commandText: args.RawCommand
            );
            _dbCtx.TpRoomInstances.Add(instance);
            _dbCtx.SaveChanges();

            _log.LogInformation("语音房间记录已保存：{RoomName}", roomName);

            _ = CreateTemporaryTextChannel(
                new TmpChannel.Core.Args.CreateTextChannelArgs(
                    "💬" + roomName,
                    parentChannel.CategoryId
                ),
                user,
                instance,
                noticeChannel
            );

            // Send post messages
            await SendRoomUpdateWizardToDmcAsync(
                tpConfig.TextChannelId == null
                    ? noticeChannel // Use the DMC created above
                    : await user.CreateDMChannelAsync() // Create new one
                , room.Name);
            await onSuccess(instance, room);
            return true;
        }
        catch (Exception e)
        {
            _log.LogError(e, "创建语音房间出错！");
            await noticeChannel.SendErrorCardAsync(ErrorMessages.ApiFailed, true);
            return false;
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
             """, false);
    }

    /// <summary>
    ///     Create temporary text channel for team play room
    /// </summary>
    /// <param name="args">Channel create args</param>
    /// <param name="creator">Team play room creator</param>
    /// <param name="room">Current team play room instance</param>
    /// <param name="noticeChannel">Notice channel</param>
    private async Task CreateTemporaryTextChannel(
        TmpChannel.Core.Args.CreateTextChannelArgs args,
        SocketGuildUser creator,
        TpRoomInstance room,
        IMessageChannel noticeChannel)
    {
        await _tmpTextChannelService.CreateAsync(args, creator,
            async (instance, newChannel) =>
            {
                room.TmpTextChannelId = instance.Id;
                _dbCtx.SaveChanges();

                await newChannel.SendSuccessCardAsync(
                    $"""
                     {MentionUtils.KMarkdownMentionUser(creator.Id)}
                     ---
                     这是属于组队房间「{room.RoomName}」的专属临时文字频道！
                     只有**加入过**语音房间的朋友才能看到该频道（即使他/她已经退出了语音）。
                     ---
                     当语音或文字房间十分钟内均无人使用时，组队房间将被删除。
                     """, false);
            },
            _ => noticeChannel.SendErrorCardAsync(FailToCreateTmpTextChannel, false));
    }
}
