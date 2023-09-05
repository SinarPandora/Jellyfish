using Jellyfish.Core.Data;
using Jellyfish.Core.Kook.Protocol;

namespace Jellyfish.Module.TeamPlay.Data;

/// <summary>
///     Team play feature config
/// </summary>
public class TpConfig : TrackableEntity
{
    public TpConfig(string name, ulong guildId)
    {
        Name = name;
        GuildId = guildId;
        Enabled = true;
    }

    public long Id { get; set; }
    public string Name { get; set; }
    public ulong? VoiceChannelId { get; set; }
    public ulong? TextChannelId { get; set; }
    public ulong GuildId { get; set; }
    public VoiceQuality? VoiceQuality { get; set; }
    public string? RoomNamePattern { get; set; }
    public int DefaultMemberLimit { get; set; }
    public bool Enabled { get; set; }

    // References
    public ICollection<TpRoomInstance> RoomInstances { get; set; } = new List<TpRoomInstance>();
}
