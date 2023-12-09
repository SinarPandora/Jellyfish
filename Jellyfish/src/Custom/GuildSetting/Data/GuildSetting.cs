using Jellyfish.Core.Data;

namespace Jellyfish.Custom.GuildSetting.Data;

/// <summary>
///     Guild setting
/// </summary>
public class GuildSetting(ulong guildId) : TrackableEntity
{
    public long Id { get; set; }
    public ulong GuildId { get; set; } = guildId;
    public GuildSettingDetails Setting { get; set; } = new();
}
