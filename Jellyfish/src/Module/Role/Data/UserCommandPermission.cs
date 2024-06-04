namespace Jellyfish.Module.Role.Data;

/// <summary>
///     Record which role of user can run what command
/// </summary>
public class UserCommandPermission(long userRoleId, string commandName)
{
    public long Id { get; set; }
    public long UserRoleId { get; init; } = userRoleId;
    public string CommandName { get; init; } = commandName;

    // References
    public UserRole UserRole { get; set; } = null!;
}
