using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfish.Data;

[Table("team_play_config")]
public class TpConfig : TrackableEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public ulong ChannelId { get; set; }
    public bool Enabled { get; set; }

    // References
    public ICollection<TpRoomInstance> RoomInstances { get; set; } = new List<TpRoomInstance>();

}
