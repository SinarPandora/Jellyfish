namespace Jellyfish.Core.Command;

/// <summary>
///     Base command interface
/// </summary>
public abstract class Command
{
    /// <summary>
    ///     Is Command enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     Name of command
    /// </summary>
    /// <returns>Command name</returns>
    public abstract string Name();
}
