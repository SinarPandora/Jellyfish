using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfish.Module.Board.Data;

/// <summary>
///     Board history
/// </summary>
public class BoardItemHistory(long itemId, ulong userId)
{
    public Guid Id { get; set; }
    public long ItemId { get; init; } = itemId;
    public ulong UserId { get; init; } = userId;
    [Column(TypeName = "timestamp")] public DateTime CreateTime { get; init; } = DateTime.Now;

    // Reference
    public BoardItem Item = null!;
}
