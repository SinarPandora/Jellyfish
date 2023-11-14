using JetBrains.Annotations;
using Refit;

namespace Jellyfish.Client.SendouInk.Response;

/// <summary>
///     Weapon data in suite
/// </summary>
[UsedImplicitly]
public class BuildAdoptWeapon
{
    [AliasAs("maxPower")] public double? MaxPower { get; set; }
    [AliasAs("minRank")] public uint? MinRank { get; set; }
    [AliasAs("weaponSplId")] public uint WeaponSplId { get; set; }
}
