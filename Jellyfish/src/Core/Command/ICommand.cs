namespace Jellyfish.Core.Command;

/// <summary>
///     Base command interface
/// </summary>
public interface ICommand
{
    /// <summary>
    ///     Name of command
    /// </summary>
    /// <returns>Command name</returns>
    string Name();
}
