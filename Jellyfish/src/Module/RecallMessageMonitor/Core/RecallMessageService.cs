using Jellyfish.Core.Data;
using Jellyfish.Module.RecallMessageMonitor.Data;
using Kook;

namespace Jellyfish.Module.RecallMessageMonitor.Core;

/// <summary>
///     Service to record the message which need recalled.
/// </summary>
public class RecallMessageService(DbContextProvider dbProvider)
{
    /// <summary>
    ///     Watch the given message and ensure it recalled eventually.
    /// </summary>
    /// <param name="messageId">Message id</param>
    /// <param name="channel">Current text channel id</param>
    public async Task WatchMessage(Guid messageId, ITextChannel channel)
    {
        await using var dbCtx = dbProvider.Provide();
        dbCtx.RecallMessages.Add(new RecallMessage(channel.GuildId, channel.Id, messageId));
        dbCtx.SaveChanges();
    }
}
