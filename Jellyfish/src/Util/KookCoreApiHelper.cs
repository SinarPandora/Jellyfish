using System.ComponentModel;
using Kook;
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

    #endregion

    private static readonly Logger Log = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

    /// <summary>
    ///     Do not modify the value of the instance, as it is assigned during the initialization period
    /// </summary>
    internal static KookSocketClient Kook = null!;

    /// <summary>
    ///     Delete single channel in guild.
    ///     The deleted channel should never be used in system.
    /// </summary>
    /// <param name="guild">Current guild</param>
    /// <param name="channelId">Channel id</param>
    /// <param name="type">Channel type</param>
    /// <exception cref="InvalidEnumArgumentException">Throws when channel type is unsupported</exception>
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
                        $"删除频道 API 调用失败一次，房间名：{channel.Name}，重试次数：{args.AttemptNumber}");
                    channel = type == ChannelType.Text
                        ? await restGuild.GetTextChannelAsync(channelId)
                        : await restGuild.GetVoiceChannelAsync(channelId);
                }
            })
            .Build()
            .ExecuteAsync(async _ =>
            {
                if (channel != null)
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
                if (currentChannel == null) return;
                if (currentChannel.Id != toChannel.Id)
                {
                    await guild.MoveUsersAsync(guildUsers, toChannel);
                    // Delay 5s for desktop or mobile app to let user join
                    await Task.Delay(TimeSpan.FromSeconds(MoveUserChannelTimeout), token);
                    currentChannel = (await restUser.GetConnectedVoiceChannelsAsync()).FirstOrDefault();
                    if (currentChannel == null || currentChannel.Id != toChannel.Id)
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
                if (restGuildChannel.GetPermissionOverwrite(role) == null)
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
                if (restGuildChannel.GetPermissionOverwrite(user) == null)
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
                if (restGuildChannel.GetPermissionOverwrite(user) != null)
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
                if (restGuildChannel.GetPermissionOverwrite(role) != null)
                {
                    await restGuildChannel.RemovePermissionOverwriteAsync(role);
                }
            });
    }

    /// <summary>
    ///     Create text channel
    /// </summary>
    /// <param name="guild">Current guild</param>
    /// <param name="name"></param>
    /// <param name="categoryId">Category to place the text channel, default is null</param>
    /// <returns></returns>
    public static async Task<RestTextChannel> CreateTextChannelAsync(this SocketGuild guild, string name,
        ulong? categoryId = null)
    {
        return await new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = 2,
                DelayGenerator =
                    PollyHelper.ProgressiveDelayGenerator(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1)),
                OnRetry = args =>
                {
                    Log.Warn(args.Outcome.Exception,
                        $"创建文字频道 API 调用失败一次，频道名：{name}，所属分类 Id：{categoryId}，重试次数：{args.AttemptNumber}");
                    return ValueTask.CompletedTask;
                }
            })
            .Build()
            .ExecuteAsync(async _ => await guild.CreateTextChannelAsync(name, c => c.CategoryId = categoryId));
    }

    /// <summary>
    ///     Get user display name(auto detect by type)
    /// </summary>
    /// <param name="user">User object</param>
    /// <returns>Display name</returns>
    public static string DisplayName(this IUser user)
    {
        return user is IGuildUser guildUser
            ? guildUser.Nickname ?? guildUser.Username
            : user.Username;
    }
}
