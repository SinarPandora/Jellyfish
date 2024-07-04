using Jellyfish.Module.GuildSetting.Enum;

namespace Jellyfish.Module.GuildSetting.Data;

/// <summary>
///     JSON type setting details
/// </summary>
// ReSharper disable CollectionNeverUpdated.Global Reason: Managed by EF Core json
public class GuildSettingDetails
{
    /// <summary>
    ///     Enabled custom features
    /// </summary>
    public HashSet<GuildCustomFeature> EnabledFeatures { get; set; } = [];

    /// <summary>
    ///     Synergy Bot account means that the bot works in tandem with the jellyfish,
    ///     and they will automatically allow access to all (temporary) channels managed by the jellyfish
    /// </summary>
    public HashSet<ulong> SynergyBotAccounts { get; set; } = [];

    /// <summary>
    ///     Synergy Bot conflict message will be deleted directly
    ///     if the message is sent by a synergy Bot
    /// </summary>
    public HashSet<string> SynergyBotConflictMessage { get; set; } = [];

    /// <summary>
    ///     Default manager accounts, by default, all commands starting with an "!" are only authorized
    ///     to these accounts, unless manually authorized using the 权限配置指令 directive
    /// </summary>
    public HashSet<ulong> DefaultManagerAccounts { get; set; } = [];

    /// <summary>
    ///     Similar with DefaultManagerAccounts, but roles
    /// </summary>
    /// <see cref="DefaultManagerAccounts"/>
    public HashSet<ulong> DefaultManagerRoles { get; set; } = [];
}
