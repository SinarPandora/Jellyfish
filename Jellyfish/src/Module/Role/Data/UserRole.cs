using Kook.WebSocket;
using LanguageExt;

namespace Jellyfish.Module.Role.Data;

/// <summary>
///     Kook user role
/// </summary>
public class UserRole
{
    public UserRole(uint kookId, ulong guildId)
    {
        KookId = kookId;
        GuildId = guildId;
    }

    public long Id { get; set; }
    public uint KookId { get; set; }
    public ulong GuildId { get; set; }
    public bool? Enabled { get; set; } = true;

    public ICollection<UserCommandPermission> CommandPermissions = new List<UserCommandPermission>();

    /// <summary>
    ///     Get kook role using cache from Kook client
    /// </summary>
    /// <param name="client">Kook client</param>
    /// <returns>Socket role object</returns>
    public Option<SocketRole> GetKookRole(KookSocketClient client) =>
    (
        from guild in client.Guilds
        where guild.Id == GuildId
        from role in guild.Roles
        where role.Id == KookId
        select role
    ).FirstOrDefault();

    /// <summary>
    ///     Get role name using cache from Kook client
    /// </summary>
    /// <param name="client">Kook client</param>
    /// <returns>Role name in guild</returns>
    public string GetName(KookSocketClient client) =>
        GetKookRole(client).Match(r => r.Name, "未找到");
}
