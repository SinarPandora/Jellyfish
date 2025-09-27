using System.ComponentModel;
using Kook;
using Kook.WebSocket;

namespace Jellyfish.Util;

/// <summary>
///     Helper methods for Kook SocketGuild object
/// </summary>
public static class GuildHelper
{
    /// <summary>
    ///     Get highest VoiceQuality in the current guild
    /// </summary>
    /// <param name="guild">Current guild</param>
    /// <exception cref="ArgumentOutOfRangeException">Throws when boost level unsupported</exception>
    /// <returns>VoiceQuality value</returns>
    public static VoiceQuality GetHighestVoiceQuality(this SocketGuild guild)
    {
        return guild.BoostLevel switch
        {
            BoostLevel.None => VoiceQuality.Normal,
            BoostLevel.Level1 => VoiceQuality.High,
            BoostLevel.Level2 => VoiceQuality.Higher,
            BoostLevel.Level3 or BoostLevel.Level4 => VoiceQuality.Excellent,
            BoostLevel.Level5 or BoostLevel.Level6 => VoiceQuality.Ultimate,
            _ => throw new InvalidEnumArgumentException($"不支持的助力等级{guild.BoostLevel}")
        };
    }
}
