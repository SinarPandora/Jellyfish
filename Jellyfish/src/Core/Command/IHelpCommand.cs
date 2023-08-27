namespace Jellyfish.Core.Command;

/// <summary>
///     Help command, which can show help insturction message
/// </summary>
public interface IHelpCommand
{
    /// <summary>
    ///     Help message
    /// </summary>
    /// <returns>Help message</returns>
    string Help();
}
