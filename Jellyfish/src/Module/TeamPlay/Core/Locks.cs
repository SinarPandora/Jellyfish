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
    public static readonly ConcurrentDictionary<ulong, DateTime> RoomCreationLock = new();

    /// <summary>
    ///     Room creation lock timeout
    /// </summary>
    public const int RoomCreationLockTimeout = 20;
}
