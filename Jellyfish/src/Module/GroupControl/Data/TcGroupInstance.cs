using Jellyfish.Core.Data;

namespace Jellyfish.Module.GroupControl.Data;

/// <summary>
///     Room group instance
/// </summary>
public class TcGroupInstance : TrackableEntity
{
    public TcGroupInstance(long tcGroupId, string name, ulong textChannelId)
    {
        Name = name;
        TextChannelId = textChannelId;
        TcGroupId = tcGroupId;
    }

    public TcGroupInstance(long tcGroupId, string name, ulong textChannelId, string? description,
        Guid? descriptionMessageId)
    {
        Name = name;
        TextChannelId = textChannelId;
        Description = description;
        DescriptionMessageId = descriptionMessageId;
        TcGroupId = tcGroupId;
    }

    public long Id { get; set; }
    public long TcGroupId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public Guid? DescriptionMessageId { get; set; }
    public ulong TextChannelId { get; set; }

    // Reference
    public TcGroup Group = null!;
}
