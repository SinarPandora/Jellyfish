namespace Jellyfish.Custom.Splatoon.Data;

/// <summary>
///     Splatoon Weapon Data
/// </summary>
public record Weapon(
    int SendouInkId,
    string SendouSlug,
    string Name,
    HashSet<string> Alias
);
