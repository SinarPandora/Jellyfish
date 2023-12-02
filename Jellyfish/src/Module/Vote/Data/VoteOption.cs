using Jellyfish.Core.Data;

namespace Jellyfish.Module.Vote.Data;

/// <summary>
///     Vote option
/// </summary>
public class VoteOption : TrackableEntity
{
    public VoteOption(string text, long voteId, ulong creatorId, long voteChannelId, bool isFree = false)
    {
        Text = text;
        VoteId = voteId;
        CreatorId = creatorId;
        VoteChannelId = voteChannelId;
        IsFree = isFree;
    }

    public long Id { get; set; }
    public required string Text { get; set; }
    public required long VoteId { get; set; }
    public required ulong CreatorId { get; set; }
    public required long VoteChannelId { get; set; }
    public required bool IsFree { get; set; }

    // Reference
    public Vote Config { get; set; } = null!;
    public VoteChannel CreationChannel { get; set; } = null!;
}
