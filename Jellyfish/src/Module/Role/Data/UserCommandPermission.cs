namespace Jellyfish.Module.Role.Data;

/// <summary>
///     Record which role of user can run what command
/// </summary>
public class UserCommandPermission(long userRoleId, string commandName)
{
    public long Id { get; set; }
    public long UserRoleId { get; set; } = userRoleId;
    public string CommandName { get; set; } = commandName;

    // References
    public UserRole UserRole { get; set; } = null!;
}
