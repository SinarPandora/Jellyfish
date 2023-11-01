namespace Jellyfish.Module.TmpChannel.Core;

/// <summary>
///     Action args
/// </summary>
public static class Args
{
    /// <summary>
    ///     Create text channel args
    /// </summary>
    /// <param name="Name">Channel name</param>
    /// <param name="CategoryId"></param>
    /// <param name="Duration">Expire duration</param>
    public record CreateTextChannelArgs(
        string Name,
        ulong? CategoryId = null,
        TimeSpan? Duration = null
    );
}
