using Jellyfish.Core.Data;

namespace Jellyfish.Module.Board.Data;

/// <summary>
///     Board instance
/// </summary>
public class BoardInstance(long configId, ulong guildId, ulong channelId, Guid messageId)
    : TrackableEntity
{
    public long Id { get; set; }
    public long ConfigId { get; init; } = configId;
    public ulong GuildId { get; init; } = guildId;
    public ulong ChannelId { get; init; } = channelId;
    public Guid MessageId { get; set; } = messageId;

    // Reference
    public BoardConfig Config = null!;
}
