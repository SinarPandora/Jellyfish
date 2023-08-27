namespace Jellyfish.Command.Role.Data;

/// <summary>
///     Kook user role
/// </summary>
public class UserRole
{
    public UserRole(string name, ulong kookId, ulong guildId)
    {
        Name = name;
        KookId = kookId;
        GuildId = guildId;
    }

    public long Id { get; set; }
    public string Name { get; set; }
    public ulong KookId { get; set; }
    public ulong GuildId { get; set; }
    public bool? Enabled { get; set; } = true;

    public ICollection<UserCommandPermission> CommandPermissions = new List<UserCommandPermission>();
}
