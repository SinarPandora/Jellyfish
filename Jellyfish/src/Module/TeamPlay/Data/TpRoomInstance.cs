using Jellyfish.Core.Data;
using Jellyfish.Module.TmpChannel.Data;

namespace Jellyfish.Module.TeamPlay.Data;

/// <summary>
///     Team play room instance
/// </summary>
public class TpRoomInstance(
    long tpConfigId,
    ulong voiceChannelId,
    ulong guildId,
    string roomName,
    ulong ownerId,
    string commandText
) : TrackableEntity
{
    public long Id { get; set; }
    public long TpConfigId { get; set; } = tpConfigId;
    public ulong VoiceChannelId { get; init; } = voiceChannelId;
    public long? TmpTextChannelId { get; set; }
    public ulong GuildId { get; init; } = guildId;
    public string RoomName { get; set; } = roomName;
    public ulong OwnerId { get; set; } = ownerId;
    public string CommandText { get; set; } = commandText;

    // References
    public TpConfig TpConfig { get; set; } = null!;
    public TmpTextChannel? TmpTextChannel { get; set; }
}
