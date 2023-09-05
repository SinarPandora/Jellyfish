using Jellyfish.Core.Data;

namespace Jellyfish.Module.TeamPlay.Data;

/// <summary>
///     Team play room instance
/// </summary>
public class TpRoomInstance : TrackableEntity
{
    public TpRoomInstance(long tpConfigId, ulong voiceChannelId, string roomName, ulong ownerId, int? memberLimit,
        string commandText)
    {
        TpConfigId = tpConfigId;
        VoiceChannelId = voiceChannelId;
        RoomName = roomName;
        OwnerId = ownerId;
        MemberLimit = memberLimit;
        CommandText = commandText;
    }

    public long Id { get; set; }
    public long TpConfigId { get; set; }
    public ulong VoiceChannelId { get; set; }
    public string RoomName { get; set; }
    public ulong OwnerId { get; set; }
    public int? MemberLimit { get; set; }
    public string CommandText { get; set; }

    // References
    public TpConfig TpConfig { get; set; } = null!;
}
