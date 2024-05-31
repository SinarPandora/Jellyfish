using System.ComponentModel.DataAnnotations;
using Jellyfish.Core.Data;

namespace Jellyfish.Module.Board.Data;

/// <summary>
///     Board item
/// </summary>
public class BoardItem(long configId, string name, string color, string buttonId)
    : TrackableEntity
{
    public long Id { get; set; }
    public long ConfigId { get; init; } = configId;
    public string Name { get; set; } = name;
    [MaxLength(6)] public string Color { get; set; } = color;
    public string ButtonId { get; set; } = buttonId;
    public uint CountCache { get; set; }

    // References
    public BoardConfig Config = null!;
    public ICollection<BoardItemHistory> Histories = new List<BoardItemHistory>();
}
