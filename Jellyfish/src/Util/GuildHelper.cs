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
            BoostLevel.None => VoiceQuality._48kbps,
            BoostLevel.Level1 => VoiceQuality._128kbps,
            BoostLevel.Level2 => VoiceQuality._192kbps,
            BoostLevel.Level3 or BoostLevel.Level4 => VoiceQuality._256kbps,
            BoostLevel.Level5 or BoostLevel.Level6 => VoiceQuality._320kbps,
            _ => throw new InvalidEnumArgumentException($"不支持的助力等级{guild.BoostLevel}")
        };
    }
}
