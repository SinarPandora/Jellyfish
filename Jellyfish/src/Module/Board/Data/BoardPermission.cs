namespace Jellyfish.Module.Board.Data;

/// <summary>
///     Board permission
/// </summary>
public class BoardPermission(long configId, string name, ulong kookId, bool isRole)
{
    public long Id { get; set; }
    public long ConfigId { get; init; } = configId;
    public string Name { get; init; } = name;
    public ulong KookId { get; init; } = kookId;
    public bool IsRole { get; init; } = isRole;

    // Reference
    public BoardConfig Config = null!;
}
