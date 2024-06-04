using Jellyfish.Core.Data;

namespace Jellyfish.Module.GroupControl.Data;

/// <summary>
///     Group of text channels
/// </summary>
public class TcGroup(string name, ulong guildId) : TrackableEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = name;
    public ulong GuildId { get; init; } = guildId;

    // References
    public ICollection<TcGroupInstance> GroupInstances { get; set; } = new List<TcGroupInstance>();
}
