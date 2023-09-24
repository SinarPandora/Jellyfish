using System.Text.RegularExpressions;
using Jellyfish.Module.TeamPlay.Data;

namespace Jellyfish.Module.TeamPlay.Core;

/// <summary>
///     Parse create room command
/// </summary>
public static partial class CreateRoomCommandParser
{
    [GeneratedRegex(@"\s*((?<limit>\d+|无限制)\s+)?(?<name>\S.+)?""")]
    private static partial Regex MatchCreateRoomCommand();

    /// <summary>
    ///     Parse command to parameter
    /// </summary>
    /// <param name="rawCommand"></param>
    /// <returns></returns>
    public static Func<TpConfig, Args.CreateRoomArgs> Parse(string rawCommand)
    {
        var match = MatchCreateRoomCommand().Match(rawCommand);
        var limit = match.Groups["limit"].Length == 0 ? null : match.Groups["limit"].Value;
        if (limit == "无限制")
        {
            limit = "0";
        }

        var name = match.Groups["name"].Value.Trim();
        name = name.Length == 0 ? null : name;

        return config => new Args.CreateRoomArgs(config, name, limit);
    }
}
