using System.ComponentModel;
using Kook;
using Kook.Net;
using Kook.Rest;
using Kook.WebSocket;
using NLog;
using NLog.Web;
using Polly;
using Polly.Retry;

namespace Jellyfish.Util;

/// <summary>
///     The core Kook API in the system, for minimizes errors, retry mechanism is configured;
///     Use the Rest API for some API instead of reading from Socket Cached
///     to avoid critical operational errors caused by caching.
/// </summary>
public static class KookCoreApiHelper
{
    #region CONST

    private const int MoveUserChannelTimeout = 5;
    public const string HasBeenBlockedByUser = "已被对方屏蔽";

    #endregion

    private static readonly Logger Log = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

    /// <summary>
    ///     Do not modify the value of the instance, as it is assigned during the initialization period
    /// </summary>
    internal static KookSocketClient Kook = null!;

    /// <summary>
    ///     Sends a card message to this message channel.
    ///     [Safe] Ignore cases where the bot is blocked.
    /// </summary>
    /// <see cref="IMessageChannel.SendCardAsync"/>
    public static async Task<Cacheable<IUserMessage, Guid>?> SendCardSafeAsync(
        this IMessageChannel channel,
        ICard card,
        IQuote? quote = null,
        IUser? ephemeralUser = null,
        RequestOptions? options = null)
    {
        try
        {
            return await channel.SendCardAsync(card, quote, ephemeralUser, options);
        }
        catch (HttpException e)
        {
            if (e.Reason.IsNotNullOrEmpty() && e.Reason!.Contains(HasBeenBlockedByUser))
            {
                Log.Warn("消息发送失败，Bot 已被对方屏蔽；该问题已被忽略，您可以从上下文中查找对应用户信息");
            }
            else
            {
                Log.Warn(e, "消息发送失败，遇到未知网络问题");
            }

            return null;
        }
    }

    /// <summary>
    ///     Sends a text message to this message channel.
    ///     [Safe] Ignore cases where the bot is blocked.
    /// </summary>
    /// <see cref="IMessageChannel.SendTextAsync"/>
    public static async Task<Cacheable<IUserMessage, Guid>?> SendTextSafeAsync(
        this IMessageChannel channel,
        string text,
        IQuote? quote = null,
        IUser? ephemeralUser = null,
        RequestOptions? options = null)
    {
        try
        {
            return await channel.SendTextAsync(text, quote, ephemeralUser, options);
        }
        catch (HttpException e)
        {
            if (e.Reason.IsNotNullOrEmpty() && e.Reason!.Contains(HasBeenBlockedByUser))
            {
                Log.Warn("消息发送失败，Bot 已被对方屏蔽；该问题已被忽略，您可以从上下文中查找对应用户信息");
            }

            return null;
        }
    }

    /// <summary>
    ///     Delete a single channel in guild.
    ///     The deleted channel should never be used in system.
    /// </summary>
    /// <param name="guild">Current guild</param>
    /// <param name="channelId">Channel id</param>
    /// <param name="type">Channel type</param>
    /// <exception cref="InvalidEnumArgumentException">Throws when the channel type is unsupported</exception>
    public static async Task DeleteSingleChannelAsync(this IGuild guild, ulong channelId, ChannelType type)
    {
        if (type != ChannelType.Text && type != ChannelType.Voice)
            throw new InvalidEnumArgumentException($"删除频道 API 不支持该类型的频道：{type}");
        var restGuild = await Kook.Rest.GetGuildAsync(guild.Id);
        RestGuildChannel? channel = type == ChannelType.Text
            ? await restGuild.GetTextChannelAsync(channelId)
            : await restGuild.GetVoiceChannelAsync(channelId);
        await new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = 2,
                DelayGenerator = PollyHelper.DefaultProgressiveDelayGenerator,
                OnRetry = async args =>
                {
                    Log.Warn(args.Outcome.Exception,
                        $"删除频道 API 调用失败一次，房间名：{channel?.Name ?? "已删除"}，重试次数：{args.AttemptNumber}");
                    channel = type == ChannelType.Text
                        ? await restGuild.GetTextChannelAsync(channelId)
                        : await restGuild.GetVoiceChannelAsync(channelId);
                }
            })
            .Build()
            .ExecuteAsync(async _ =>
            {
                if (channel is not null)
                {
                    await channel.DeleteAsync();
                }
            });
    }

    /// <summary>
    ///     Move user to room, core API, retry in 2 times if error occur
    /// </summary>
    /// <param name="guild">Current guild</param>
    /// <param name="userId">Target user id</param>
    /// <param name="toChannel">To channel</param>
    public static async Task MoveToRoomAsync(this IGuild guild, ulong userId, IVoiceChannel toChannel)
    {
        var restGuild = await Kook.Rest.GetGuildAsync(guild.Id);
        var restUser = await restGuild.GetUserAsync(userId);
        var currentChannel = (await restUser.GetConnectedVoiceChannelsAsync()).FirstOrDefault();
        var guildUsers = new List<IGuildUser> { restUser };
        await new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = 2,
                DelayGenerator = PollyHelper.DefaultProgressiveDelayGenerator,
                OnRetry = async args =>
                {
                    Log.Warn(args.Outcome.Exception,
                        $"移动频道 API 调用失败一次，用户名：{restUser.DisplayName()}，房间名：{toChannel.Name}，重试次数：{args.AttemptNumber}");
                    restUser = await restGuild.GetUserAsync(userId);
                    currentChannel = (await restUser.GetConnectedVoiceChannelsAsync()).FirstOrDefault();
                }
            })
            .Build()
            .ExecuteAsync(async token =>
            {
                if (currentChannel is null) return;
                if (currentChannel.Id != toChannel.Id)
                {
                    await guild.MoveUsersAsync(guildUsers, toChannel);
                    // Delay 5s for desktop or mobile app to let user join
                    await Task.Delay(TimeSpan.FromSeconds(MoveUserChannelTimeout), token);
                    currentChannel = (await restUser.GetConnectedVoiceChannelsAsync()).FirstOrDefault();
                    if (currentChannel is null || currentChannel.Id != toChannel.Id)
                    {
                        throw new ApplicationException("当前用户可能未成功移动至语音频道，尝试重新移动用户");
                    }
                }
            });
    }

    /// <summary>
    ///     Override role permission for channel
    /// </summary>
    /// <param name="channel">Target channel</param>
    /// <param name="role">Target role</param>
    /// <param name="overrideFn">Override function</param>
    public static async Task OverrideRolePermissionAsync(this IGuildChannel channel, IRole role,
        Func<OverwritePermissions, OverwritePermissions> overrideFn)
    {
        var restGuild = await Kook.Rest.GetGuildAsync(role.Guild.Id);
        var restGuildChannel = await restGuild.GetChannelAsync(channel.Id);
        await new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = 2,
                DelayGenerator = PollyHelper.DefaultProgressiveDelayGenerator,
                OnRetry = async args =>
                {
                    Log.Warn(args.Outcome.Exception,
                        $"覆写频道角色权限 API 调用失败一次，角色名：{role.Name}，频道名：{restGuildChannel.Name}，重试次数：{args.AttemptNumber}");
                    restGuildChannel = await restGuild.GetChannelAsync(channel.Id);
                }
            })
            .Build()
            .ExecuteAsync(async _ =>
            {
                if (restGuildChannel.GetPermissionOverwrite(role) is null)
                {
                    await restGuildChannel.AddPermissionOverwriteAsync(role);
                }

                await restGuildChannel.ModifyPermissionOverwriteAsync(role, overrideFn);
            });
    }

    /// <summary>
    ///     Override user permission for channel
    /// </summary>
    /// <param name="channel">Target channel</param>
    /// <param name="user">Target user</param>
    /// <param name="overrideFn">Override function</param>
    public static async Task OverrideUserPermissionAsync(this IGuildChannel channel, IGuildUser user,
        Func<OverwritePermissions, OverwritePermissions> overrideFn)
    {
        var restGuild = await Kook.Rest.GetGuildAsync(user.Guild.Id);
        var restGuildChannel = await restGuild.GetChannelAsync(channel.Id);
        await new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = 2,
                DelayGenerator = PollyHelper.DefaultProgressiveDelayGenerator,
                OnRetry = async args =>
                {
                    Log.Warn(args.Outcome.Exception,
                        $"覆写频道角色权限 API 调用失败一次，用户名：{user.DisplayName}，频道名：{restGuildChannel.Name}，重试次数：{args.AttemptNumber}");
                    restGuildChannel = await restGuild.GetChannelAsync(channel.Id);
                }
            })
            .Build()
            .ExecuteAsync(async _ =>
            {
                if (restGuildChannel.GetPermissionOverwrite(user) is null)
                {
                    await restGuildChannel.AddPermissionOverwriteAsync(user);
                }

                await restGuildChannel.ModifyPermissionOverwriteAsync(user, overrideFn);
            });
    }

    /// <summary>
    ///     Remove user permission override for channel
    /// </summary>
    /// <param name="channel">Target channel</param>
    /// <param name="user">Target user</param>
    public static async Task RemoveUserPermissionOverrideAsync(this IGuildChannel channel, IGuildUser user)
    {
        var restGuild = await Kook.Rest.GetGuildAsync(user.Guild.Id);
        var restGuildChannel = await restGuild.GetChannelAsync(channel.Id);
        await new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = 2,
                DelayGenerator = PollyHelper.DefaultProgressiveDelayGenerator,
                OnRetry = async args =>
                {
                    Log.Warn(args.Outcome.Exception,
                        $"删除覆盖的频道角色权限 API 调用失败一次，用户名：{user.DisplayName}，频道名：{restGuildChannel.Name}，重试次数：{args.AttemptNumber}");
                    restGuildChannel = await restGuild.GetChannelAsync(channel.Id);
                }
            })
            .Build()
            .ExecuteAsync(async _ =>
            {
                if (restGuildChannel.GetPermissionOverwrite(user) is not null)
                {
                    await restGuildChannel.RemovePermissionOverwriteAsync(user);
                }
            });
    }

    /// <summary>
    ///     Remove user permission override for channel
    /// </summary>
    /// <param name="channel">Target channel</param>
    /// <param name="role">Target user</param>
    public static async Task RemoveRolePermissionOverrideAsync(this IGuildChannel channel, IRole role)
    {
        var restGuild = await Kook.Rest.GetGuildAsync(role.Guild.Id);
        var restGuildChannel = await restGuild.GetChannelAsync(channel.Id);
        await new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = 2,
                DelayGenerator = PollyHelper.DefaultProgressiveDelayGenerator,
                OnRetry = async args =>
                {
                    Log.Warn(args.Outcome.Exception,
                        $"删除覆盖的频道角色权限 API 调用失败一次，权限名：{role.Name}，频道名：{restGuildChannel.Name}，重试次数：{args.AttemptNumber}");
                    restGuildChannel = await restGuild.GetChannelAsync(channel.Id);
                }
            })
            .Build()
            .ExecuteAsync(async _ =>
            {
                if (restGuildChannel.GetPermissionOverwrite(role) is not null)
                {
                    await restGuildChannel.RemovePermissionOverwriteAsync(role);
                }
            });
    }

    /// <summary>
    ///     Delete message, check if it exists before action.
    /// </summary>
    /// <param name="channel">Current text channel</param>
    /// <param name="messageId">Message id</param>
    public static async Task DeleteMessageSafeAsync(this RestTextChannel channel, Guid messageId)
    {
        await new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle =
                    new PredicateBuilder().Handle<Exception>(ex =>
                        ex is not HttpException || !ex.Message.Contains("40012")),
                MaxRetryAttempts = 2,
                DelayGenerator = PollyHelper.DefaultProgressiveDelayGenerator,
                OnRetry = args =>
                {
                    Log.Warn(args.Outcome.Exception,
                        $"删除消息 API 调用失败一次，频道：{channel.Guild.Name}，房间名：{channel.Name}，消息 ID：{messageId}，重试次数：{args.AttemptNumber}");
                    return ValueTask.CompletedTask;
                }
            })
            .Build()
            .ExecuteAsync(async _ =>
            {
                try
                {
                    var message = (RestMessage?)await channel.GetMessageAsync(messageId);
                    if (message is null) return;

                    await message.DeleteAsync();

                    message = await channel.GetMessageAsync(messageId);
                    if (message is not null)
                    {
                        throw new Exception(
                            $"本应删除的消息依然存在，频道：{channel.Guild.Name}，房间名：{channel.Name}，消息 ID：{messageId}");
                    }
                }
                catch (HttpException e)
                {
                    if (e.Message.Contains("40012"))
                    {
                        // Ignore errors which get message throws
                        return;
                    }

                    throw;
                }
            });
    }

    /// <summary>
    ///     Get user display name(auto detect by type)
    /// </summary>
    /// <param name="user">User object</param>
    /// <returns>Display name</returns>
    public static string DisplayName(this IUser user)
    {
        return user is IGuildUser guildUser ? guildUser.DisplayName : user.Username;
    }
}
