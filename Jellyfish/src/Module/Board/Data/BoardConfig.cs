using System.ComponentModel.DataAnnotations.Schema;
using Jellyfish.Core.Data;

namespace Jellyfish.Module.Board.Data;

/// <summary>
///     Board configuration
/// </summary>
public class BoardConfig(
    ulong guildId,
    bool isTemplate,
    string title,
    string details,
    DateTime due,
    BoardType boardType)
    : TrackableEntity
{
    public long Id { get; set; }
    public ulong GuildId { get; init; } = guildId;
    public bool IsTemplate { get; init; } = isTemplate;
    public string Title { get; set; } = title;
    public string Details { get; set; } = details;
    [Column(TypeName = "timestamp")] public DateTime Due { get; set; } = due;
    public BoardType BoardType { get; init; } = boardType;
    public bool Finished { get; set; }

    // References
    public ICollection<BoardItem> Items { get; set; } = new List<BoardItem>();
    public ICollection<BoardInstance> Instances { get; set; } = new List<BoardInstance>();
    public ICollection<BoardPermission> Permissions { get; set; } = new List<BoardPermission>();
}
