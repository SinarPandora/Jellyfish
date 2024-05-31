namespace Jellyfish.Module.ClockIn.Data;

/// <summary>
///     Channel for clock-in
///     Manager can set to show only clock-in messages or allow users to clock-in in the current channel.
///     When the associate config is default, user can clock-in in any channel event manager set clock-in channel.
/// </summary>
public class ClockInChannel(long configId, ulong kookId, bool isSync)
{
    public long Id { get; set; }
    public long ConfigId { get; set; } = configId;
    public ulong KookId { get; set; } = kookId;

    /// <summary>
    ///     Whether to synchronize the sending of clock-in messages
    /// </summary>
    public bool IsSync { get; set; } = isSync;

    public Guid? MessageId { get; set; }

    /// <summary>
    ///     Enable check-In or not
    /// </summary>
    public bool ReadOnly { get; set; } = false;

    // References
    public ClockInConfig Config { get; set; } = null!;
}
