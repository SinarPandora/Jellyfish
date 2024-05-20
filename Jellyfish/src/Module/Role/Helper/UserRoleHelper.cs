using Jellyfish.Module.Role.Data;
using Kook.WebSocket;

namespace Jellyfish.Module.Role.Helper;

/// <summary>
///     Helper method for user role
/// </summary>
public static class UserRoleHelper
{
    /// <summary>
    ///     Get a kook role using cache from the Kook client
    /// </summary>
    /// <param name="role">User role</param>>
    /// <param name="guild">Current guild</param>
    /// <returns>Socket role object</returns>
    public static SocketRole? GetKookRole(this UserRole role, SocketGuild guild) =>
    (
        from guildRole in guild.Roles
        where guildRole.Id == role.KookId
        select guildRole
    ).FirstOrDefault();

    /// <summary>
    ///     Get role name using cache from Kook client
    /// </summary>
    /// <param name="role">User role</param>>
    /// <param name="guild">Current guild</param>
    /// <returns>Role name in guild</returns>
    public static string GetName(this UserRole role, SocketGuild guild) =>
        GetKookRole(role, guild)?.Name ?? "未找到";

    /// <summary>
    ///     Get guild role id by name
    /// </summary>
    /// <param name="guild">Guild to search</param>
    /// <param name="name">Role name</param>
    /// <returns>Role id or null</returns>
    public static uint? GetRoleIdByName(this SocketGuild guild, string name) =>
    (
        from role in guild.Roles
        where role.Name == name
        select role.Id
    ).FirstOrDefault();
}
