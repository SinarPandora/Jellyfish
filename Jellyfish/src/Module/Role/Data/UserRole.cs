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
        Enabled = true;
    }

    public long Id { get; set; }
    public uint KookId { get; set; }
    public ulong GuildId { get; set; }
    public bool Enabled { get; set; }

    public ICollection<UserCommandPermission> CommandPermissions = new List<UserCommandPermission>();
}
