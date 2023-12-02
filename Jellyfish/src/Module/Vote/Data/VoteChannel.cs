using Jellyfish.Core.Data;

namespace Jellyfish.Module.Vote.Data;

/// <summary>
///     Channel for vote
/// </summary>
public class VoteChannel : TrackableEntity
{
    public VoteChannel(long voteId, ulong guildId, ulong channelId, bool votable, bool synced, bool enableFree = false)
    {
        VoteId = voteId;
        GuildId = guildId;
        ChannelId = channelId;
        Votable = votable;
        Synced = synced;
        EnableFree = enableFree;
    }

    public long Id { get; set; }
    public required long VoteId { get; set; }
    public required ulong GuildId { get; set; }
    public required ulong ChannelId { get; set; }
    public required bool Votable { get; set; }
    public required bool Synced { get; set; }
    public ulong? MessageId { get; set; }
    public required bool EnableFree { get; set; }

    // Reference
    public Vote Config { get; set; } = null!;
    public ICollection<VoteOption> Options { get; set; } = null!;
}
