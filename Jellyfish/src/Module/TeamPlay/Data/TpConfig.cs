using Jellyfish.Core.Data;

namespace Jellyfish.Module.TeamPlay.Data;

/// <summary>
///     Team play feature config
/// </summary>
public class TpConfig(string name, ulong guildId) : TrackableEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = name;
    public ulong? VoiceChannelId { get; set; }
    public ulong? TextChannelId { get; set; }
    public ulong? VoiceCategoryId { get; set; }
    public ulong? TextCategoryId { get; set; }
    public ulong? CreationNotifyChannelId { get; set; }
    public ulong GuildId { get; set; } = guildId;
    public string? RoomNamePattern { get; set; }
    public int DefaultMemberLimit { get; set; }
    public bool Enabled { get; set; } = true;

    // References
    public ICollection<TpRoomInstance> RoomInstances { get; set; } = new List<TpRoomInstance>();
}
