using Kook.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Jellyfish.Module.TeamPlay.Data;

/// <summary>
///     Team play config query helper
/// </summary>
public static class TpConfigHelper
{
    public static IQueryable<TpConfig> EnabledInGuild(
        this DbSet<TpConfig> query,
        SocketGuild guild
    ) => query.Where(e => e.GuildId == guild.Id && e.Enabled);
}
