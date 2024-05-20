using JetBrains.Annotations;
using Refit;

namespace Jellyfish.Client.SendouInk.Model;

/// <summary>
///     Suite build filter
/// </summary>
[UsedImplicitly]
public class BuildFilter(string ability, string comparison, string value)
{
    public const string AtMost = "AT_MOST";
    public const string AtLeast = "AT_LEAST";

    [AliasAs("ability")] public string Ability { get; set; } = ability;
    [AliasAs("comparison")] public string Comparison { get; set; } = comparison;
    [AliasAs("value")] public string Value { get; set; } = value;
}
