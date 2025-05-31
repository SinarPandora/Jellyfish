using Jellyfish.Core.Data;

namespace Jellyfish.Module.RecallMessageMonitor.Data;

/// <summary>
///     Messages, which should be recalled
/// </summary>
public class RecallMessage(ulong guildId, ulong channelId, Guid messageId) : TrackableEntity
{
    public Guid Id { get; set; }
    public ulong GuildId { get; set; } = guildId;
    public ulong ChannelId { get; set; } = channelId;
    public Guid MessageId { get; set; } = messageId;
}
