using Jellyfish.Core.Data;
using Jellyfish.Module.TmpChannel.Data;

namespace Jellyfish.Module.TeamPlay.Data;

/// <summary>
///     Team play room instance
/// </summary>
public class TpRoomInstance : TrackableEntity
{
    public TpRoomInstance(long tpConfigId, ulong voiceChannelId, ulong guildId, string roomName, ulong ownerId,
        string commandText)
    {
        TpConfigId = tpConfigId;
        VoiceChannelId = voiceChannelId;
        GuildId = guildId;
        RoomName = roomName;
        OwnerId = ownerId;
        CommandText = commandText;
    }

    public long Id { get; set; }
    public long TpConfigId { get; set; }
    public ulong VoiceChannelId { get; set; }
    public long? TmpTextChannelId { get; set; }
    public ulong GuildId { get; set; }
    public string RoomName { get; set; }
    public ulong OwnerId { get; set; }
    public string CommandText { get; set; }

    // References
    public TpConfig TpConfig { get; set; } = null!;
    public TmpTextChannel? TmpTextChannel { get; set; }
}
