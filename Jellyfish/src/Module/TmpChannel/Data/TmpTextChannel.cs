using Jellyfish.Core.Data;

namespace Jellyfish.Module.TmpChannel.Data;

/// <summary>
///     Temporary text channel instance
/// </summary>
public class TmpTextChannel : TrackableEntity
{
    public TmpTextChannel(ulong guildId, ulong channelId, string name, ulong creatorId, DateTime? expireTime)
    {
        GuildId = guildId;
        ChannelId = channelId;
        Name = name;
        CreatorId = creatorId;
        ExpireTime = expireTime;
    }

    public long Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public string Name { get; set; }
    public ulong CreatorId { get; set; }
    public DateTime? ExpireTime { get; set; }
}
