using Jellyfish.Core.Cache;
using Jellyfish.Core.Command;
using Kook;
using Kook.WebSocket;

namespace Jellyfish.Module.Role.Core;

/// <summary>
///     Helper class for user permission
/// </summary>
public static class PermissionHelper
{
    /// <summary>
    ///     If the user can execute this command
    /// </summary>
    /// <param name="user">User</param>
    /// <param name="command">Guild message command</param>
    /// <returns>Is user can execute command or not</returns>
    public static bool CanExecute(this SocketGuildUser user, GuildMessageCommand command)
    {
        var commandPerm = AppCaches.Permissions.GetValueOrDefault($"{user.Guild.Id}_{command.Name()}");

        return commandPerm.IsNullOrEmpty() // Is permission unset?
            ? !command.IsManagerCommand || IsJellyfishManager(user)
            : user.Roles.Any(role => commandPerm!.Contains(role.Id));
    }

    /// <summary>
    ///     Is user a manager of Jellyfish bot
    /// </summary>
    /// <param name="user">Any user</param>
    /// <returns>Is manager or not</returns>
    private static bool IsJellyfishManager(SocketGuildUser user)
    {
        var setting = AppCaches.GuildSettings[user.Guild.Id];
        var hasSetManager = setting.DefaultManagerAccounts.IsNotEmpty() || setting.DefaultManagerRoles.IsNotEmpty();
        return hasSetManager
            ? setting.DefaultManagerAccounts.Contains(user.Id) ||
              user.Roles.Any(role => setting.DefaultManagerRoles.Contains(role.Id))
            : user.Roles.Any(role => role.Permissions.Has(GuildPermission.Administrator));
    }
}
