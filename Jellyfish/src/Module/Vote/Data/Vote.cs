using Jellyfish.Core.Data;

namespace Jellyfish.Module.Vote.Data;

/// <summary>
///     Vote config
/// </summary>
public class Vote : TrackableEntity
{
    public Vote(string name, string title, string details, ulong managerId, bool isFree)
    {
        Name = name;
        Title = title;
        Details = details;
        ManagerId = managerId;
        IsFree = isFree;
    }

    public long Id { get; set; }
    public required string Name { get; set; }
    public required string Title { get; set; }
    public required string Details { get; set; }
    public required ulong ManagerId { get; set; }
    public required bool IsFree { get; set; }

    // Reference
    public ICollection<VoteOption> Options { get; set; } = new List<VoteOption>();
    public ICollection<VoteChannel> Channels { get; set; } = new List<VoteChannel>();
}
