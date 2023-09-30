using Jellyfish.Core.Data;

namespace Jellyfish.Module.GroupControl.Data;

/// <summary>
///     Group of rooms
/// </summary>
public class RoomGroup : TrackableEntity
{
    public RoomGroup(string name)
    {
        Name = name;
        Hidden = true;
    }

    public long Id { get; set; }
    public string Name { get; set; }
    public bool Hidden { get; set; }
}
