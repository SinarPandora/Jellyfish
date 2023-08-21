using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfish.Data;

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
    public bool Enabled { get; set; } = true;

    // References
    public ICollection<TpRoomInstance> RoomInstances { get; set; } = new List<TpRoomInstance>();
}
