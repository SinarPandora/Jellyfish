using Jellyfish.Core.Data;

namespace Jellyfish.Module.ClockIn.Data;

/// <summary>
///     Config for clock in
/// </summary>
public class ClockInConfig(ulong guildId, string name, string title) : TrackableEntity
{
    public long Id { get; set; }
    public ulong GuildId { get; set; } = guildId;
    public string Name { get; set; } = name;
    public string Title { get; set; } = title;
    public string? Description { get; set; }
    public string ButtonText { get; set; } = "打卡！";
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     All stage result will be send to this channel
    /// </summary>
    public ulong? ResultChannelId { get; set; }

    /// <summary>
    ///     If this config is the default, any channel that sends the clock-in command
    ///     will execute this clock-in. And only one channel can be default in a guild.
    /// </summary>
    public bool IsDefault { get; set; }

    // References
    public ICollection<ClockInChannel> Channels { get; set; } = null!;
    public ICollection<ClockInStage> Stages { get; set; } = null!;
    public ICollection<ClockInHistory> Histories { get; set; } = null!;
}
