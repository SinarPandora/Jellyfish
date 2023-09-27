using System.Text.RegularExpressions;
using Jellyfish.Module.TeamPlay.Data;

namespace Jellyfish.Module.TeamPlay.Core;

/// <summary>
///     Parse create room command
/// </summary>
public static partial class CreateRoomCommandParser
{
    [GeneratedRegex(@"^((?<limit>\d+|无限制)\s+)?(?<name>.+?)(\s+(?<password>\d+))?$")]
    private static partial Regex MatchAllInOneCommand();

    [GeneratedRegex(@"(?<type>人数|密码)\s+(?<value>\d+)")]
    private static partial Regex MatchSimpleArg();

    /// <summary>
    ///     Parse command to parameter
    /// </summary>
    /// <param name="rawCommand"></param>
    /// <returns>Create room argument builder with parsed args</returns>
    public static Func<TpConfig, Args.CreateRoomArgs> Parse(string rawCommand)
    {
        if (rawCommand.IsEmpty()) return config => new Args.CreateRoomArgs(config, string.Empty);
        return rawCommand.ContainsAny("人数", "密码")
            ? ParseSimpleCommand(rawCommand)
            : ParseAllInOneCommand(rawCommand);
    }

    private static Func<TpConfig, Args.CreateRoomArgs> ParseSimpleCommand(string rawCommand)
    {
        string? memberLimit = null;
        string? password = null;

        foreach (Match match in MatchSimpleArg().Matches(rawCommand))
        {
            if (match.Groups["type"].Value == "人数")
            {
                memberLimit = match.Groups["value"].Value;
            }
            else
            {
                password = match.Groups["value"].Value;
            }
        }

        return config => new Args.CreateRoomArgs(config, rawCommand, null, memberLimit, password ?? string.Empty);
    }

    private static Func<TpConfig, Args.CreateRoomArgs> ParseAllInOneCommand(string rawCommand)
    {
        var match = MatchAllInOneCommand().Match(rawCommand);
        var limit = match.Groups["limit"].Length == 0 ? null : match.Groups["limit"].Value;
        if (limit == "无限制")
        {
            limit = "0";
        }

        var name = match.Groups["name"].Value;
        name = name.Length == 0 ? null : name;

        var password = match.Groups["password"].Value;

        return config => new Args.CreateRoomArgs(config, rawCommand, name, limit, password);
    }
}
