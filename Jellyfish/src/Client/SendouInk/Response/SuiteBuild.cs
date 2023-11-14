using JetBrains.Annotations;
using Refit;

namespace Jellyfish.Client.SendouInk.Response;

/// <summary>
///     Suite data
/// </summary>
[UsedImplicitly]
public class SuiteBuild
{
    [AliasAs("id")] public ulong Id { get; set; }
    [AliasAs("title")] public string Title { get; set; } = null!;
    [AliasAs("abilities")] public string[][] Abilities { get; set; } = null!;
    [AliasAs("description")] public string? Description { get; set; }
    [AliasAs("discordDiscriminator")] public string? DiscordDiscriminator { get; set; }
    [AliasAs("discordId")] public string? DiscordId { get; set; }
    [AliasAs("discordName")] public string? DiscordName { get; set; }
    [AliasAs("isTop500")] public int IsTop500 { get; set; }
    [AliasAs("modes")] public string[] Modes { get; set; } = null!;
    [AliasAs("ownerId")] public ulong OwnerId { get; set; }
    [AliasAs("plusTier")] public string? PlusTier { get; set; }
    [AliasAs("headGearSplId")] public uint HeadGearSplId { get; set; }
    [AliasAs("clothesGearSplId")] public uint ClothesGearSplId { get; set; }
    [AliasAs("shoesGearSplId")] public uint ShoesGearSplId { get; set; }
    [AliasAs("updatedAt")] public DateTime UpdatedAt { get; set; }
    [AliasAs("weapons")] public BuildAdoptWeapon[] Weapons { get; set; } = null!;
}
