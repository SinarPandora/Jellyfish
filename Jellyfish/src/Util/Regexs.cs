using System.Text.RegularExpressions;

namespace Jellyfish.Util;

/// <summary>
///     Common Regex(use for code generating)
/// </summary>
public abstract partial class Regexs
{
    [GeneratedRegex(@"\s*[，,]\s*")]
    public static partial Regex MatchSpaceComma();

    [GeneratedRegex(@"\s+")]
    public static partial Regex MatchWhiteChars();

    [GeneratedRegex("_")]
    public static partial Regex MatchSingleDash();

    [GeneratedRegex(@"\(chn\)(?<channelId>\d+)\(chn\)")]
    public static partial Regex MatchTextChannelMention();
}
