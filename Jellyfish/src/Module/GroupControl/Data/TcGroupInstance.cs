using Jellyfish.Core.Data;

namespace Jellyfish.Module.GroupControl.Data;

/// <summary>
///     Room group instance
/// </summary>
public class TcGroupInstance(
    long tcGroupId,
    string name,
    ulong textChannelId,
    string? description = null,
    Guid? descriptionMessageId = null)
    : TrackableEntity
{
    public long Id { get; set; }
    public long TcGroupId { get; init; } = tcGroupId;
    public string Name { get; set; } = name;
    public string? Description { get; set; } = description;
    public Guid? DescriptionMessageId { get; set; } = descriptionMessageId;
    public ulong TextChannelId { get; set; } = textChannelId;

    // Reference
    public TcGroup Group = null!;
}
