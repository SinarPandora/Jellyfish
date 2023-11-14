using Jellyfish.Client.SendouInk.Model;
using JetBrains.Annotations;
using Refit;

namespace Jellyfish.Client.SendouInk.Response;

/// <summary>
///     Suite record
/// </summary>
[UsedImplicitly]
public class BuildsRecord
{
    [AliasAs("builds")] public SuiteBuild[] Builds { get; set; } = null!;
    [AliasAs("filters")] public BuildFilter[] Filters { get; set; } = null!;
    [AliasAs("limit")] public uint Limit { get; set; }
    [AliasAs("slug")] public string Slug { get; set; } = null!;
    [AliasAs("title")] public string Title { get; set; } = null!;
    [AliasAs("weaponId")] public uint WeaponId { get; set; }
    [AliasAs("weaponName")] public string WeaponName { get; set; } = null!;
}
