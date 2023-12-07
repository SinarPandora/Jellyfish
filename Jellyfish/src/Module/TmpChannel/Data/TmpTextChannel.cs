using Jellyfish.Core.Data;

namespace Jellyfish.Module.TmpChannel.Data;

/// <summary>
///     Temporary text channel instance
/// </summary>
public class TmpTextChannel(ulong guildId, ulong channelId, string name, ulong creatorId, DateTime? expireTime)
    : TrackableEntity
{
    public long Id { get; set; }
    public ulong GuildId { get; set; } = guildId;
    public ulong ChannelId { get; set; } = channelId;
    public string Name { get; set; } = name;
    public ulong CreatorId { get; set; } = creatorId;
    public DateTime? ExpireTime { get; set; } = expireTime;
}
