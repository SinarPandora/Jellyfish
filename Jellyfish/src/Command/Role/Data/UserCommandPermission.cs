namespace Jellyfish.Command.Role.Data;

/// <summary>
///     Record which role of user can run what command
/// </summary>
public class UserCommandPermission
{
    public UserCommandPermission(long userRoleId, string commandName)
    {
        UserRoleId = userRoleId;
        CommandName = commandName;
    }

    public long Id { get; set; }
    public long UserRoleId { get; set; }
    public string CommandName { get; set; }

    // References
    public UserRole UserRole { get; set; } = null!;
}
