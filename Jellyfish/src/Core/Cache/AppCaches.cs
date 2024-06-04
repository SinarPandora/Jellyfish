using System.Collections.Concurrent;
using Jellyfish.Module.ClockIn.Data;
using Jellyfish.Module.GuildSetting.Data;
using Jellyfish.Module.TeamPlay.Data;

namespace Jellyfish.Core.Cache;

/// <summary>
///     Application Caches
/// </summary>
public static class AppCaches
{
    /// <summary>
    ///     User Role Cache, GuildId_CommandName as the key, a set of user role as value
    /// </summary>
    public static readonly ConcurrentDictionary<string, HashSet<uint>> Permissions = [];

    /// <summary>
    ///     Team Play Configuration Cache, GuildId_ConfigName as the key, config object(without tracking) as value
    /// </summary>
    public static readonly ConcurrentDictionary<string, TpConfig> TeamPlayConfigs = [];

    /// <summary>
    ///     Guild Setting Cache, guild id as the key, setting details as value
    /// </summary>
    public static readonly ConcurrentDictionary<ulong, GuildSettingDetails> GuildSettings = [];

    /// <summary>
    ///     Clock-In Config Cache, guild id as the key, config (with stages) as value
    /// </summary>
    public static readonly ConcurrentDictionary<ulong, ClockInConfig> ClockInConfigs = [];
}
