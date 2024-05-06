using System.Collections.Concurrent;

namespace Jellyfish.Module.TeamPlay.Core;

/// <summary>
///     Team play global locks
/// </summary>
public abstract class Locks
{
    /// <summary>
    ///     Room creation lock
    /// </summary>
    public static readonly ConcurrentDictionary<string, DateTime> RoomCreationLock = new();

    /// <summary>
    ///     Room creation lock timeout
    /// </summary>
    private const int RoomCreationLockTimeout = 120;

    /// <summary>
    ///     Check if user is currently locked by CreationLock
    ///     Lock duration: 10s for each user
    /// </summary>
    /// <param name="userId">Action user Id</param>
    /// <param name="configId">Current TP config Id</param>
    /// <returns>If is locked</returns>
    public static bool IsUserBeLockedByCreationLock(ulong userId, long configId)
    {
        var lockKey = $"{configId}_{userId}";
        if (RoomCreationLock.TryGetValue(lockKey, out var timestamp))
        {
            if (timestamp.AddSeconds(RoomCreationLockTimeout) > DateTime.Now) return true;

            RoomCreationLock.Remove(lockKey, out _);
        }
        else RoomCreationLock[lockKey] = DateTime.Now;

        return false;
    }
}
