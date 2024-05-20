using Jellyfish.Core.Data;
using Jellyfish.Module.TmpChannel.Data;
using Jellyfish.Util;
using Kook.Rest;
using Kook.WebSocket;
using Polly;
using Polly.Retry;

namespace Jellyfish.Module.TmpChannel.Core;

/// <summary>
///     Service to create the temporary text channel to complement the missing features of Kook
/// </summary>
public class TmpTextChannelService(
    ILogger<TmpTextChannelService> log,
    DbContextProvider dbProvider,
    BaseSocketClient kook)
{
    /// <summary>
    ///     Create temporary text room with given permission.
    ///     No need to check if owner already has a text channel, limit temporary text room only
    ///     by controlling the timing of the method call.
    /// </summary>
    /// <param name="args">Actually arguments</param>
    /// <param name="creator">The creator</param>
    /// <param name="permissionSetupFn">Permission setup function</param>
    /// <param name="onSuccess">Action on success</param>
    /// <param name="onError">Action on error</param>
    public async Task CreateAsync(Args.CreateTextChannelArgs args, SocketGuildUser creator,
        Func<RestTextChannel, Task> permissionSetupFn,
        Func<TmpTextChannel, RestTextChannel, Task> onSuccess,
        Func<Exception, Task> onError)
    {
        var identityStr = $"房间名：{args.Name}，创建者：{creator.DisplayName()}#{creator.Id}";
        DateTime? expireTime = args.Duration.HasValue ? DateTime.Now.Add(args.Duration.Value) : null;
        log.LogInformation("开始创建临时文字频道 {IdentityStr}", identityStr);

        try
        {
            await using var dbCtx = dbProvider.Provide();
            var newChannel = await CreateTmpTextChannelAsync(creator.Guild, args.Name, args.CategoryId, dbCtx);

            log.LogInformation("开始设置临时文字频道 {IdentityStr} 的权限", identityStr);
            await permissionSetupFn(newChannel);
            log.LogInformation("临时文字频道 {IdentityStr} 权限设置完成", identityStr);

            var instance = new TmpTextChannel(
                guildId: newChannel.GuildId,
                channelId: newChannel.Id,
                name: args.Name,
                creatorId: creator.Id,
                expireTime: expireTime
            );

            dbCtx.TmpTextChannels.Add(instance);
            dbCtx.SaveChanges();

            log.LogInformation("临时文字频道创建成功，{IdentityStr}，房间 ID：{ChannelId}，过期时间：{Now}",
                identityStr, newChannel.Id, expireTime?.ToString() ?? "永久");
            await onSuccess(instance, newChannel);
        }
        catch (Exception e)
        {
            log.LogError(e, "临时文字频道创建失败，{IdentityStr}", identityStr);
            await onError(e);
        }
    }

    /// <summary>
    ///     Create temp text channel in kook
    /// </summary>
    /// <param name="guild">Current guild</param>
    /// <param name="name"></param>
    /// <param name="categoryId">Category to place the text channel, default is null</param>
    /// <param name="dbCtx">Active database context</param>
    /// <returns>Created room</returns>
    private async Task<RestTextChannel> CreateTmpTextChannelAsync(SocketGuild guild, string name,
        ulong? categoryId, DatabaseContext dbCtx)
    {
        var restGuild = await kook.Rest.GetGuildAsync(guild.Id);
        var existingChannelIds = GetExistingChannelIdsByName(guild, name, dbCtx);
        return await new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = 2,
                DelayGenerator =
                    PollyHelper.ProgressiveDelayGenerator(TimeSpan.FromSeconds(3), TimeSpan.FromMinutes(1)),
                OnRetry = args =>
                {
                    log.LogWarning(args.Outcome.Exception,
                        "创建文字频道 API 调用失败一次，频道名：{Name}，所属分组 Id：{CategoryId}，重试次数：{ArgsAttemptNumber}",
                        name, categoryId, args.AttemptNumber);
                    existingChannelIds = GetExistingChannelIdsByName(guild, name, dbCtx);
                    return ValueTask.CompletedTask;
                }
            })
            .Build()
            .ExecuteAsync(async _ =>
            {
                var existingChannel =
                    (from channel in restGuild.TextChannels
                        where channel.Name == name
                              && channel.CategoryId == categoryId
                              && !existingChannelIds.Contains(channel.Id)
                        select channel)
                    .FirstOrDefault();

                return existingChannel ?? await restGuild.CreateTextChannelAsync(name, c => c.CategoryId = categoryId);
            });
    }

    /// <summary>
    ///     Get existing channels' id as hash-set by channel name.
    ///     A known channel that is used to avoid duplicate names,
    ///     during a failed retry process is misidentified as a newly created channel
    /// </summary>
    /// <param name="guild">Current guild</param>
    /// <param name="name">Channel name</param>
    /// <param name="dbCtx">Current database context</param>
    /// <returns>Channel ids in hash-set</returns>
    private static HashSet<ulong> GetExistingChannelIdsByName(SocketGuild guild, string name, DatabaseContext dbCtx)
    {
        return (from channel in dbCtx.TmpTextChannels
                where channel.GuildId == guild.Id && channel.Name == name
                select channel.ChannelId)
            .ToHashSet();
    }
}
