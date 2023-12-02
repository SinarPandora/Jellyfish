using Jellyfish.Core.Data;

namespace Jellyfish.Module.Vote.Data;

/// <summary>
///     Vote option
/// </summary>
public class VoteOption : TrackableEntity
{
    public VoteOption(string text, ulong voteId, ulong creatorId, ulong voteChannelId, bool isFree = false)
    {
        Text = text;
        VoteId = voteId;
        CreatorId = creatorId;
        VoteChannelId = voteChannelId;
        IsFree = isFree;
    }

    public ulong Id { get; set; }
    public required string Text { get; set; }
    public required ulong VoteId { get; set; }
    public required ulong CreatorId { get; set; }
    public required ulong VoteChannelId { get; set; }
    public required bool IsFree { get; set; }

    // Reference
    public Vote Config { get; set; } = null!;
    public VoteChannel CreationChannel { get; set; } = null!;
}
