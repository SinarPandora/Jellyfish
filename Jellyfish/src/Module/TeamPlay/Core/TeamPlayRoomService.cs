using Jellyfish.Core.Cache;
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
public class TeamPlayRoomService(
    ILogger<TeamPlayRoomService> log,
    TmpTextChannelService tmpTextChannelService,
    DbContextProvider dbProvider)
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

    /// <summary>
    ///     Create room instance, using text command
    /// </summary>
    /// <param name="args">Create room args</param>
    /// <param name="user">Current user</param>
    /// <param name="noticeChannel">Text channel for notice</param>
    /// <param name="onSuccess">Callback on success</param>
    /// <returns>Should keep the user message or not</returns>
    public async Task<bool> CreateAndMoveToRoomAsync(
        Args.CreateRoomArgs args, SocketGuildUser user,
        IMessageChannel? noticeChannel,
        Func<TpRoomInstance, RestVoiceChannel, RestTextChannel?, Task> onSuccess)
    {
        if (Locks.IsUserBeLockedByCreationLock(user.Id, args.Config.Id))
        {
            _ = Task.Run(async () =>
            {
                var dmc = await user.CreateDMChannelAsync();
                await dmc.SendWarningCardAsync(
                    """
                    您在两分钟内多次尝试创建同类型的组队房间，请使用「已创建好」的组队房间
                    或等待两分钟冷却结束再进行操作。
                    ---
                    ❓找不到刚刚创建的语音/文字房间？
                    > 这可能是由于 Kook 的客户端缓存有延迟

                    1. 请尝试重启 Kook 客户端（网页版请尝试刷新页面）
                    2. 如果您加入过其他的 Kook 服务器，可尝试进入其他服务器后再回来
                    """, true, TimeSpan.FromMinutes(2));
            });
            return true;
        }

        var tpConfig = args.Config;

        var guild = user.Guild;
        noticeChannel ??= await user.CreateDMChannelAsync();

        var roomName = tpConfig.RoomNamePattern is not null
            ? tpConfig.RoomNamePattern.Replace(TeamPlayManageService.UserInjectKeyword,
                args.RoomName ?? user.DisplayName)
            : args.RoomName ?? $"{user.DisplayName}的房间";
        var roomNameWithoutIcon = roomName;

        var isVoiceChannelHasPassword = args.Password.IsNotEmpty();
        if (isVoiceChannelHasPassword)
        {
            if (args.Password.Length > 12 || !long.TryParse(args.Password, out _))
            {
                await noticeChannel.SendErrorCardAsync(UnsupportedPassword, true);
                return false;
            }

            roomName = $"🔐{roomName}";
        }
        else
        {
            roomName = $"🔊{roomName}";
        }

        await using var dbCtx = dbProvider.Provide();
        if (dbCtx.TpRoomInstances.Any(e => e.OwnerId == user.Id))
        {
            log.LogInformation("创建频道 {RoomName} 失败，用户 {DisplayName}#{UserId} 已加入其他语音频道", roomName, user.DisplayName,
                user.Id);
            await noticeChannel.SendErrorCardAsync(UserDoesNotFree, true);
            return false;
        }

        var voiceCategoryId = GetVoiceCategoryId(tpConfig, user.Guild);
        var textCategoryId = GetTextCategoryId(tpConfig, user.Guild);
        if (!voiceCategoryId.HasValue)
        {
            log.LogError("{TpConfigId}：{TpConfigName} 所对应的父频道未找到，请检查错误日志并更新频道配置", tpConfig.Id, tpConfig.Name);
            await noticeChannel.SendErrorCardAsync(ParentChannelNotFound, true);
            return false;
        }

        int? memberLimit;
        if (args.RawMemberLimit is not null)
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
            log.LogInformation("开始创建语音房间{RoomName}", roomName);
            var voiceChannel = await guild.CreateVoiceChannelAsync(roomName, r =>
            {
                r.VoiceQuality = guild.GetHighestVoiceQuality();
                r.UserLimit = memberLimit;
                r.CategoryId = voiceCategoryId.Value;
            });

            if (isVoiceChannelHasPassword)
            {
                log.LogInformation("检测到房间 {RoomName} 带有初始密码，尝试设置密码", roomName);
                await voiceChannel.ModifyAsync(v => v.Password = args.Password);
                log.LogInformation("房间 {RoomName} 密码设置成功！", roomName);
            }

            // Give owner permission
            await GiveOwnerPermissionAsync(voiceChannel, user);

            log.LogInformation("创建语音房间 API 调用成功，房间名：{RoomName}", roomName);

            log.LogInformation("尝试移动用户所在房间，用户：{DisplayName}，目标房间：{RoomName}", user.DisplayName(), voiceChannel.Name);

            if (user.VoiceChannel is not null)
            {
                // Ignore error for moving user to the voice channel
                _ = guild.MoveToRoomAsync(user.Id, voiceChannel);
            }

            var instance = new TpRoomInstance(
                tpConfigId: tpConfig.Id,
                voiceChannelId: voiceChannel.Id,
                guildId: tpConfig.GuildId,
                roomName: roomName,
                ownerId: user.Id,
                commandText: args.RawCommand
            );
            dbCtx.TpRoomInstances.Add(instance);
            dbCtx.SaveChanges(); // Save for the voice channel

            log.LogInformation("语音房间记录已保存：{RoomName}", roomName);

            // Error already handled inside the method with callback
            RestTextChannel? tmpTextChannel = null;
            if (tpConfig.EnableTmpTextChannel)
            {
                tmpTextChannel = await CreateTemporaryTextChannel(
                    new TmpChannel.Core.Args.CreateTextChannelArgs(
                        (isVoiceChannelHasPassword ? "🔐" : "💬") + roomNameWithoutIcon,
                        textCategoryId ?? voiceCategoryId
                    ),
                    user, instance, voiceChannel, isVoiceChannelHasPassword, noticeChannel
                );
                dbCtx.SaveChanges(); // Save for the text channel
            }

            await onSuccess(instance, voiceChannel, tmpTextChannel);
            return true;
        }
        catch (Exception e)
        {
            log.LogError(e, "创建语音房间出错！");
            await noticeChannel.SendErrorCardAsync(ErrorMessages.ApiFailed, true);
            return false;
        }
    }

    /// <summary>
    ///     Using the configured voice channel category id,
    ///     if category does not find, using the category of bound voice channel
    /// </summary>
    /// <param name="config">Team play config</param>
    /// <param name="guild">Current guild</param>
    /// <returns>Nullable category channel id</returns>
    private static ulong? GetVoiceCategoryId(TpConfig config, SocketGuild guild)
    {
        if (config.VoiceCategoryId.HasValue)
        {
            var category = guild.GetCategoryChannel(config.VoiceCategoryId.Value);
            if (category is not null)
            {
                return category.Id;
            }
        }

        if (!config.VoiceChannelId.HasValue) return null;
        var parent = guild.GetVoiceChannel(config.VoiceChannelId.Value);
        return parent?.CategoryId;
    }

    /// <summary>
    ///     Using the configured text channel category id,
    ///     if category does not find, using the category of bound text channel
    /// </summary>
    /// <param name="config">Team play config</param>
    /// <param name="guild">Current guild</param>
    /// <returns>Nullable category channel id</returns>
    private static ulong? GetTextCategoryId(TpConfig config, SocketGuild guild)
    {
        if (config.TextCategoryId.HasValue)
        {
            var category = guild.GetCategoryChannel(config.TextCategoryId.Value);
            if (category is not null)
            {
                return category.Id;
            }
        }

        if (!config.TextChannelId.HasValue) return null;
        var parent = guild.GetTextChannel(config.TextChannelId.Value);
        return parent?.CategoryId;
    }

    /// <summary>
    ///     Give voice channel permission to user
    /// </summary>
    /// <param name="channel">Room</param>
    /// <param name="user">Owner</param>
    public static Task GiveOwnerPermissionAsync(IVoiceChannel channel, IGuildUser user)
    {
        return channel.OverrideUserPermissionAsync(user, _ => OverwritePermissions.AllowAll(channel));
    }

    /// <summary>
    ///     Create room invite card
    /// </summary>
    /// <param name="room">New voice channel</param>
    /// <returns>Kook card object</returns>
    public static async Task<Card> CreateInviteCardAsync(IVoiceChannel room)
    {
        var invite = await room.CreateInviteAsync(InviteMaxAge.NeverExpires);
        var card = new CardBuilder();
        card.AddModule<HeaderModuleBuilder>(m => m.Text = $"✅房间已创建：{room.Name}，等您加入！");
        card.AddModule<InviteModuleBuilder>(m => m.Code = invite.Code);
        card.WithSize(CardSize.Large);
        return card.Build();
    }

    /// <summary>
    ///     Create temporary text channel for team play room
    /// </summary>
    /// <param name="args">Channel create args</param>
    /// <param name="creator">Team play room creator</param>
    /// <param name="roomInstance">Current team play room instance</param>
    /// <param name="voiceChannel">Current voice channel</param>
    /// <param name="isVoiceChannelHasPassword">Is voice channel has password</param>
    /// <param name="noticeChannel">Notice channel</param>
    /// <returns>New text channel or null if fail to create</returns>
    private async Task<RestTextChannel?> CreateTemporaryTextChannel(TmpChannel.Core.Args.CreateTextChannelArgs args,
        SocketGuildUser creator,
        TpRoomInstance roomInstance,
        IVoiceChannel voiceChannel,
        bool isVoiceChannelHasPassword,
        IMessageChannel noticeChannel)
    {
        var promise = new TaskCompletionSource<RestTextChannel?>();
        await tmpTextChannelService.CreateAsync(args, creator,
            newChannel =>
            {
                // If voice channel has password, make the bound text channel also be private
                if (isVoiceChannelHasPassword)
                {
                    return Task.WhenAll(
                        newChannel.OverrideUserPermissionAsync(creator, p =>
                            p.Modify(
                                viewChannel: PermValue.Allow,
                                mentionEveryone: PermValue.Allow
                            )),
                        newChannel.OverrideRolePermissionAsync(creator.Guild.EveryoneRole, p =>
                            p.Modify(viewChannel: PermValue.Deny)
                        ),
                        Task.WhenAll(AppCaches.GuildSettings[roomInstance.GuildId].SynergyBotAccounts.Select(botId =>
                        {
                            var botUser = creator.Guild.GetUser(botId);
                            if (botUser is not null)
                            {
                                return newChannel.OverrideUserPermissionAsync(botUser, p =>
                                    p.Modify(
                                        viewChannel: PermValue.Allow,
                                        mentionEveryone: PermValue.Allow
                                    ));
                            }

                            return Task.CompletedTask;
                        }))
                    );
                }

                return Task.CompletedTask;
            },
            async (instance, newChannel) =>
            {
                roomInstance.TmpTextChannelId = instance.Id;
                roomInstance.TmpTextChannel = instance;

                await newChannel.SendSuccessCardAsync(
                    $"""
                     {MentionUtils.KMarkdownMentionUser(creator.Id)}
                     ---
                     欢迎光临！这是属于组队房间「{roomInstance.RoomName}」的专属临时文字房间！
                     （若语音房间设置了密码，该房间将改为仅语音内玩家可见）
                     ---
                     作为房主，您可以随意修改语音房间信息，设置密码，调整麦序，全体静音等
                     当语音及文字房间二十分钟内均无人使用时，组队房间将被解散。
                     ---
                     - 修改语音房间名称后，文字房间将在稍后自动同步，无需修改两次
                     - 手机 Kook APP 暂不支持设置语音房间密码
                     """, false);

                await newChannel.SendCardSafeAsync(await CreateInviteCardAsync(voiceChannel));
                await newChannel.SendTextSafeAsync("👍🏻还未加入组队语音？点击上方按钮进入对应语音房间");

                promise.SetResult(newChannel);
            },
            _ =>
            {
                promise.SetResult(null);
                return noticeChannel.SendErrorCardAsync(FailToCreateTmpTextChannel, false);
            });

        return await promise.Task;
    }
}
