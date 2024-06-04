using System.ComponentModel.DataAnnotations.Schema;
using Jellyfish.Core.Data;

namespace Jellyfish.Module.TmpChannel.Data;

/// <summary>
///     Temporary text channel instance
/// </summary>
public class TmpTextChannel(ulong guildId, ulong channelId, string name, ulong creatorId, DateTime? expireTime)
    : TrackableEntity
{
    public long Id { get; set; }
    public ulong GuildId { get; init; } = guildId;
    public ulong ChannelId { get; init; } = channelId;
    public string Name { get; set; } = name;
    public ulong CreatorId { get; init; } = creatorId;
    [Column(TypeName = "timestamp")] public DateTime? ExpireTime { get; set; } = expireTime;
}
