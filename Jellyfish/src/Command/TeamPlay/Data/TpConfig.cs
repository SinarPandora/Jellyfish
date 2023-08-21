using System.ComponentModel.DataAnnotations.Schema;
using Jellyfish.Core.Protocol;
using Jellyfish.Data;

namespace Jellyfish.Command.TeamPlay.Data;

[Table("team_play_config")]
public class TpConfig : TrackableEntity
{
    public TpConfig(string name, ulong channelId)
    {
        Name = name;
        ChannelId = channelId;
    }

    public long Id { get; set; }
    public string Name { get; set; }
    public ulong ChannelId { get; set; }
    public VoiceQuality VoiceQuality { get; set; } = VoiceQuality.MEDIUM;
    public bool? Enabled { get; set; } = true;

    // References
    public ICollection<TpRoomInstance> RoomInstances { get; set; } = new List<TpRoomInstance>();
}
