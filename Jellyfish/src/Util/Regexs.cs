using System.Text.RegularExpressions;

namespace Jellyfish.Util;

/// <summary>
///     Common Regex(use for code generating)
/// </summary>
public abstract partial class Regexs
{
    [GeneratedRegex(@"\s")]
    public static partial Regex MatchWhiteChars();
}
