using Jellyfish.Core.Data;

namespace Jellyfish.Module.GuildSetting.Data;

/// <summary>
///     Guild setting
/// </summary>
public class GuildSetting(ulong guildId) : TrackableEntity
{
    public long Id { get; set; }
    public ulong GuildId { get; init; } = guildId;
    public GuildSettingDetails Setting { get; set; } = new();
}
