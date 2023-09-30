using Jellyfish.Core.Data;

namespace Jellyfish.Module.GroupControl.Data;

/// <summary>
///     Room group instance
/// </summary>
public class RoomGroupInstance : TrackableEntity
{
    public RoomGroupInstance(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public long Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
}
