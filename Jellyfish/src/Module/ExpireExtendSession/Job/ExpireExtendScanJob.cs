using FluentScheduler;
using Jellyfish.Core.Data;
using Jellyfish.Module.ExpireExtendSession.Data;
using Jellyfish.Util;
using Kook.WebSocket;

namespace Jellyfish.Module.ExpireExtendSession.Job;

/// <summary>
///     Scan all extend session, extend expire time on condition
/// </summary>
public class ExpireExtendScanJob : IAsyncJob
{
    private readonly DbContextProvider _dbProvider;
    private readonly KookSocketClient _kook;
    private readonly ILogger<ExpireExtendScanJob> _log;

    public ExpireExtendScanJob(KookSocketClient kook,
        ILogger<ExpireExtendScanJob> log, DbContextProvider dbProvider)
    {
        _kook = kook;
        _log = log;
        _dbProvider = dbProvider;
    }

    /// <summary>
    ///     Scan expire extend session
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Throw when unsupported session found</exception>
    public async Task ExecuteAsync()
    {
        await using var dbCtx = _dbProvider.Provide();
        var now = DateTimeOffset.Now;
        foreach (var session in dbCtx.ExpireExtendSessions.ToArray())
        {
            try
            {
                switch (session.TargetType)
                {
                    case ExtendTargetType.TmpTextChannel:
                        await HandleTmpTextChannelExpire(session, now, dbCtx);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(session.TargetType), session.TargetType,
                            $"不支持的延长会话类型：{session.TargetType}");
                }
            }
            catch (Exception e)
            {
                _log.LogError(e, "延长会话处理失败，会话 ID：{SessionId}，会话目标：{TargetType}，目标 ID：{TargetId}", session.Id,
                    session.TargetType, session.TargetId);
            }
        }
    }

    /// <summary>
    ///     Check and handle if temporary text channel instance expire
    /// </summary>
    /// <param name="session">Expire extend session</param>
    /// <param name="now">Current datetime</param>
    /// <param name="dbCtx">Database context</param>
    private async Task HandleTmpTextChannelExpire(Data.ExpireExtendSession session, DateTimeOffset now,
        DatabaseContext dbCtx)
    {
        var handled = false;
        var instance = dbCtx.TmpTextChannels
            .FirstOrDefault(i => i.Id == session.TargetId);
        if (instance != null)
        {
            var textChannel = _kook.GetGuild(instance.GuildId).GetTextChannel(instance.ChannelId);
            if (textChannel != null)
            {
                var messages = await textChannel.GetMessagesAsync(1).FirstAsync();
                // Is still alive
                if (messages.IsNotEmpty() &&
                    messages.First().Timestamp.Add(session.TimeUnit.ToTimeSpan(session.Value)) > now) return;

                _log.LogInformation("监测到临时文字房间到期，开始清理房间：{Name}#{Id}", textChannel.Name, textChannel.Id);
                await textChannel.DeleteAsync();
                _log.LogInformation("在服务器中的房间实例已被清理，数据库中的房间数据将由临时房间清理任务清理");
                handled = true;
            }
        }

        dbCtx.ExpireExtendSessions.Remove(session);
        dbCtx.SaveChanges();
        _log.LogInformation("监测到延长会话{Action}，正在清理延长会话：{Id}", handled ? "已结束" : "已失效", session.Id);
    }
}
