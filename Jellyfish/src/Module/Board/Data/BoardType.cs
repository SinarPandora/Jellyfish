namespace Jellyfish.Module.Board.Data;

/// <summary>
///     Board type
/// </summary>
[Obsolete("This feature has been removed, keep this class for data migration only")]
public enum BoardType
{
    /// <summary>
    ///     Score board: there is a +1/-1 button, which can be clicked for everyone, with history
    /// </summary>
    Score,

    /// <summary>
    ///     Vote board: each user can vote for one time
    /// </summary>
    Vote,

    /// <summary>
    ///     Match board: after the referee sets the score, it enters the two-way confirmation process (default confirmation, CD can be interrupted)
    /// </summary>
    Match,
}
