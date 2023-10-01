using Jellyfish.Core.Data;

namespace Jellyfish.Module.GroupControl.Data;

/// <summary>
///     Group of text channels
/// </summary>
public class TcGroup : TrackableEntity
{
    public TcGroup(string name, ulong guildId)
    {
        Name = name;
        GuildId = guildId;
    }

    public long Id { get; set; }
    public string Name { get; set; }
    public ulong GuildId { get; set; }

    // References
    public ICollection<TcGroupInstance> GroupInstances { get; set; } = new List<TcGroupInstance>();
}
