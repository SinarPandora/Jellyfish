using Jellyfish.Data;

namespace Jellyfish.Module.TeamPlay.Data;

/// <summary>
///     Team play room instance
/// </summary>
public class TpRoomInstance : TrackableEntity
{
    public long Id { get; set; }
    public long TpConfigId { get; set; }
    public ulong VoiceChannelId { get; set; }
    public ulong CreatorId { get; set; }
    public uint MemberLimit { get; set; } = 10;

    // References
    public TpConfig TpConfig { get; set; } = null!;
}
