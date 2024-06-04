using Kook;

namespace Jellyfish.Core.Command;

/// <summary>
///     Command argument with different types
/// </summary>
public record CommandArg
{
    private record EmptyArg : CommandArg;

    /// <summary>
    ///     Empty command arg (If given input is null or a blank string)
    /// </summary>
    public static CommandArg Empty { get; } = new EmptyArg();

    /// <summary>
    ///     Simple text value
    /// </summary>
    public record Text(string Value) : CommandArg;

    /// <summary>
    ///     Id value (for table in database)
    /// </summary>
    public record Id(long Value) : CommandArg;

    /// <summary>
    ///     Integer value
    /// </summary>
    public record Int(int Value) : CommandArg;

    /// <summary>
    ///     Decimal value
    /// </summary>
    public record Decimal(decimal Value) : CommandArg;

    /// <summary>
    ///     DataOnly value
    /// </summary>
    public record Date(DateOnly Value) : CommandArg;

    /// <summary>
    ///     TimeOnly value
    /// </summary>
    public record Time(TimeOnly Value) : CommandArg;

    /// <summary>
    ///     Date and time value
    /// </summary>
    public record DateAndTime(DateTime Value) : CommandArg;

    /// <summary>
    ///     Channel reference
    /// </summary>
    public record ChannelRef(ulong Value) : CommandArg;

    /// <summary>
    ///     User reference
    /// </summary>
    public record UserRef(ulong Value) : CommandArg;

    /// <summary>
    ///     Role reference
    /// </summary>
    public record RoleRef(ulong Value) : CommandArg;

    private CommandArg()
    {
        // private constructor can prevent derived cases from being defined elsewhere
    }

    /// <summary>
    ///     Tries to parse a channel reference or id from the input string
    /// </summary>
    /// <param name="input">Input string</param>
    /// <param name="arg">Parse result</param>
    /// <returns>Is success or not</returns>
    public static bool TryParseChannelRefOrId(string input, out CommandArg arg)
    {
        if (input.IsNullOrWhiteSpace())
        {
            arg = Empty;
            return false;
        }

        if (MentionUtils.TryParseChannel(input, out var channelId, TagMode.KMarkdown))
        {
            arg = new ChannelRef(channelId);
            return true;
        }

        if (long.TryParse(input, out var id))
        {
            arg = new Id(id);
            return true;
        }

        arg = Empty;
        return false;
    }
}
