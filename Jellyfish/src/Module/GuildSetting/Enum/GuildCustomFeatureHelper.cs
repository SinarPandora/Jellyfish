namespace Jellyfish.Module.GuildSetting.Enum;

/// <summary>
///     Helper class for GuildCustomFeature
/// </summary>
public static class GuildCustomFeatureHelper
{
    /// <summary>
    ///     Enum to string
    /// </summary>
    /// <param name="feature">GuildCustomFeature enum value</param>
    /// <returns>Display name</returns>
    /// <exception cref="ArgumentOutOfRangeException">When enum is out of range</exception>
    public static string ToName(this GuildCustomFeature feature) =>
        feature switch
        {
            GuildCustomFeature.SplatoonGame => "斯普拉遁游戏",
            GuildCustomFeature.BotSplatoon3 => "斯普拉遁3Bot联动",
            _ => throw new ArgumentOutOfRangeException(nameof(feature), feature, $"不支持的附加功能：{feature}")
        };

    /// <summary>
    ///     Display name to enum
    /// </summary>
    /// <param name="name">Display name</param>
    /// <returns>GuildCustomFeature enum value</returns>
    public static GuildCustomFeature? FromName(string name) =>
        name switch
        {
            "斯普拉遁游戏" => GuildCustomFeature.SplatoonGame,
            "斯普拉遁3Bot联动" => GuildCustomFeature.BotSplatoon3,
            _ => null
        };
}
