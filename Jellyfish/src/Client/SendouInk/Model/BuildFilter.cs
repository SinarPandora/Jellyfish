using JetBrains.Annotations;
using Refit;

namespace Jellyfish.Client.SendouInk.Model;

/// <summary>
///     Suite build filter
/// </summary>
[UsedImplicitly]
public class BuildFilter
{
    public const string AtMost = "AT_MOST";
    public const string AtLeast = "AT_LEAST";

    public BuildFilter(string ability, string comparison, string value)
    {
        Ability = ability;
        Comparison = comparison;
        Value = value;
    }

    [AliasAs("ability")] public string Ability { get; set; }
    [AliasAs("comparison")] public string Comparison { get; set; }
    [AliasAs("value")] public string Value { get; set; }
}
