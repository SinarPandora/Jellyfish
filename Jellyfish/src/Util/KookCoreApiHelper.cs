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
                DelayGenerator = PollyHelper.ProgressiveDelayGenerator,
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
    /// <param name="user">Target user</param>
    /// <param name="toChannel">To channel</param>
    public static async Task MoveToRoomAsync(this IGuild guild, SocketGuildUser user, IVoiceChannel toChannel)
    {
        var currentChannel = (await user.GetConnectedVoiceChannelsAsync()).FirstOrDefault();
        var guildUsers = new List<IGuildUser> { user };
        await new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = 2,
                DelayGenerator = PollyHelper.ProgressiveDelayGenerator,
                OnRetry = async args =>
                {
                    Log.Warn(args.Outcome.Exception,
                        $"移动频道 API 调用失败一次，用户名：{user.DisplayName()}，房间名：{toChannel.Name}，重试次数：{args.AttemptNumber}");
                    currentChannel = (await user.GetConnectedVoiceChannelsAsync()).FirstOrDefault();
                }
            })
            .Build()
            .ExecuteAsync(async _ =>
            {
                if (currentChannel == null) return;
                if (currentChannel.Id != toChannel.Id)
                {
                    await guild.MoveUsersAsync(guildUsers, toChannel);
                }
            });
    }

    /// <summary>
    ///     Override role permission for channel
    /// </summary>
    /// <param name="channel">Target channel</param>
    /// <param name="role">Target role</param>
    /// <param name="overrideFn">Override function</param>
    public static async Task OverrideRolePermission(this IGuildChannel channel, IRole role,
        Func<OverwritePermissions, OverwritePermissions> overrideFn)
    {
        // Using rest client to avoid caching interfering with the results
        var restGuild = await Kook.Rest.GetGuildAsync(role.Guild.Id);
        var restGuildChannel = await restGuild.GetChannelAsync(channel.Id);
        await new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = 2,
                DelayGenerator = PollyHelper.ProgressiveDelayGenerator,
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
    public static async Task OverrideUserPermission(this IGuildChannel channel, IGuildUser user,
        Func<OverwritePermissions, OverwritePermissions> overrideFn)
    {
        // Using rest client to avoid caching interfering with the results
        var restGuild = await Kook.Rest.GetGuildAsync(user.Guild.Id);
        var restGuildChannel = await restGuild.GetChannelAsync(channel.Id);
        await new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = 2,
                DelayGenerator = PollyHelper.ProgressiveDelayGenerator,
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
                DelayGenerator = PollyHelper.ProgressiveDelayGenerator,
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
}
