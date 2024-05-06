namespace Jellyfish.Module.Role.Data;

/// <summary>
///     Kook user role
/// </summary>
public class UserRole(uint kookId, ulong guildId)
{
    public long Id { get; set; }
    public uint KookId { get; set; } = kookId;
    public ulong GuildId { get; set; } = guildId;
    public bool Enabled { get; set; } = true;

    public ICollection<UserCommandPermission> CommandPermissions = new List<UserCommandPermission>();
}
