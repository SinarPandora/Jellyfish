using System.ComponentModel.DataAnnotations.Schema;
using Jellyfish.Data;

namespace Jellyfish.Command.TeamPlay.Data;

[Table("team_play_room_instance")]
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
