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
    ///     Get highest VoiceQuality in current guild
    /// </summary>
    /// <param name="guild">Current guild</param>
    /// <exception cref="ArgumentOutOfRangeException">Throws when boost level unsupported</exception>
    /// <returns>VoiceQuality value</returns>
    public static VoiceQuality GetHighestVoiceQuality(this SocketGuild guild)
    {
        switch (guild.BoostLevel)
        {
            case BoostLevel.None:
                return VoiceQuality._48kbps;
            case BoostLevel.Level1:
                return VoiceQuality._128kbps;
            case BoostLevel.Level2:
                return VoiceQuality._192kbps;
            case BoostLevel.Level3:
            case BoostLevel.Level4:
                return VoiceQuality._256kbps;
            case BoostLevel.Level5:
            case BoostLevel.Level6:
                return VoiceQuality._320kbps;
            default:
                throw new InvalidEnumArgumentException($"不支持的助力等级{guild.BoostLevel}");
        }
    }
}
