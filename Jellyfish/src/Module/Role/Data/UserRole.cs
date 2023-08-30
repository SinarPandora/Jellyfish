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
}
