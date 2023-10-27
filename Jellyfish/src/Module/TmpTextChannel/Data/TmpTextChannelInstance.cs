using Jellyfish.Core.Data;

namespace Jellyfish.Module.TmpTextChannel.Data;

/// <summary>
///     Temporary text channel instance
/// </summary>
public class TmpTextChannelInstance : TrackableEntity
{
    public TmpTextChannelInstance(ulong guildId, string roomName, ulong creatorId, DateTime expireTime)
    {
        GuildId = guildId;
        RoomName = roomName;
        CreatorId = creatorId;
        ExpireTime = expireTime;
    }

    public long Id { get; set; }
    public ulong GuildId { get; set; }
    public string RoomName { get; set; }
    public ulong CreatorId { get; set; }
    public DateTime ExpireTime { get; set; }
}
