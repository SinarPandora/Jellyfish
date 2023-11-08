using Jellyfish.Core.Data;
using Jellyfish.Module.TmpChannel.Data;
using Jellyfish.Util;
using Kook;
using Kook.Rest;
using Kook.WebSocket;

namespace Jellyfish.Module.TmpChannel.Core;

/// <summary>
///     Service to create temporary text channel to complements the missing features of Kook
/// </summary>
public class TmpTextChannelService
{
    private readonly ILogger<TmpTextChannelService> _log;
    private readonly DatabaseContext _dbCtx;

    public TmpTextChannelService(ILogger<TmpTextChannelService> log, DatabaseContext dbCtx)
    {
        _log = log;
        _dbCtx = dbCtx;
    }


    /// <summary>
    ///     Create temporary text room, hide for everyone. Then set view permission for creator.
    ///     No need to check if owner already have a text channel, limit temporary text room only
    ///     by controlling the timing of the method call.
    /// </summary>
    /// <param name="args">Actually arguments</param>
    /// <param name="creator">The creator</param>
    /// <param name="onSuccess">Action on success</param>
    /// <param name="onError">Action on error</param>
    public async Task CreateAsync(Args.CreateTextChannelArgs args, SocketGuildUser creator,
        Func<TmpTextChannel, RestTextChannel, Task> onSuccess, Func<Exception, Task> onError)
    {
        var identityStr = $"房间名：{args.Name}，创建者：{creator.DisplayName()}#{creator.Id}";
        DateTime? expireTime = args.Duration.HasValue ? DateTime.Now.Add(args.Duration.Value) : null;
        _log.LogInformation("开始创建临时文字频道，{IdentityStr}", identityStr);

        try
        {
            var newChannel = await creator.Guild.CreateTextChannelAsync(args.Name, args.CategoryId);
            await newChannel.OverrideRolePermission(creator.Guild.EveryoneRole, p =>
                p.Modify(viewChannel: PermValue.Deny)
            );

            await newChannel.OverrideUserPermission(creator, p => p.Modify(viewChannel: PermValue.Allow));

            var instance = new TmpTextChannel(
                guildId: newChannel.GuildId,
                channelId: newChannel.Id,
                name: args.Name,
                creatorId: creator.Id,
                expireTime: expireTime
            );

            _dbCtx.TmpTextChannels.Add(instance);
            _dbCtx.SaveChanges();

            _log.LogInformation("临时文字频道创建成功，{IdentityStr}，房间 ID：{ChannelId}，过期时间：{Now}",
                identityStr, newChannel.Id, expireTime?.ToString() ?? "永久");
            await onSuccess(instance, newChannel);
        }
        catch (Exception e)
        {
            _log.LogError(e, "临时文字频道创建失败，{IdentityStr}", identityStr);
            await onError(e);
        }
    }
}
