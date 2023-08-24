namespace Jellyfish.Core.Protocol;

/// <summary>
///     Voice quality enum names
/// </summary>
public abstract class VoiceQualityNames
{
    /// <summary>
    ///     Get name of VoiceQuality enum value
    /// </summary>
    /// <param name="quality">Enum value</param>
    /// <returns>Voice quality name</returns>
    /// <exception cref="ArgumentOutOfRangeException">Throws when input that is not a valid enum value</exception>
    public static string Get(VoiceQuality quality)
    {
        return quality switch
        {
            VoiceQuality.Low => "低",
            VoiceQuality.Medium => "中",
            VoiceQuality.High => "高",
            _ => throw new ArgumentOutOfRangeException(nameof(quality), quality, $"内部错误：非法的语音质量枚举值：{quality}")
        };
    }

    /// <summary>
    ///     Get VoiceQuality enum value from name
    /// </summary>
    /// <param name="name">Voice quality name</param>
    /// <returns>Voice</returns>
    public static VoiceQuality? FromName(string name)
    {
        return name switch
        {
            "低" => VoiceQuality.Low,
            "中" => VoiceQuality.Medium,
            "高" => VoiceQuality.High,
            _ => null
        };
    }
}
