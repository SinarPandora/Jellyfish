using Kook;
using Kook.WebSocket;

namespace Jellyfish.Core.Kook.Protocol;

/// <summary>
///     Voice quality enum helper
/// </summary>
public static class VoiceQualityHelper
{
    /// <summary>
    ///     Get name of VoiceQuality enum value
    /// </summary>
    /// <param name="quality">Enum value</param>
    /// <returns>Voice quality name</returns>
    /// <exception cref="ArgumentOutOfRangeException">Throws when input that is not a valid enum value</exception>
    public static string GetName(this VoiceQuality quality)
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
    /// <returns>VoiceQuality value</returns>
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

    /// <summary>
    ///     Get highest VoiceQuality in current guild
    /// </summary>
    /// <param name="guild">Current guild</param>
    /// <returns>VoiceQuality value</returns>
    public static VoiceQuality GetHighestVoiceQuality(this SocketGuild guild) =>
        guild.BoostLevel > BoostLevel.None ? VoiceQuality.High : VoiceQuality.Medium;
}
