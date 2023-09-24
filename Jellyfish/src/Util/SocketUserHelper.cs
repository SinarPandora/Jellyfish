using Kook.WebSocket;

namespace Jellyfish.Util;

/// <summary>
///     Helper class for socket user
/// </summary>
public static class SocketUserHelper
{
    /// <summary>
    ///     Get user display name(auto detect by type)
    /// </summary>
    /// <param name="user">User object</param>
    /// <returns>Display name</returns>
    public static string DisplayName(this SocketUser user)
    {
        return user is SocketGuildUser guildUser
            ? guildUser.Nickname ?? guildUser.Username
            : user.Username;
    }
}
