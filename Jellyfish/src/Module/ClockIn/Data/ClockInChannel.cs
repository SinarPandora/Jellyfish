namespace Jellyfish.Module.ClockIn.Data;

/// <summary>
///     Channel for displaying the clock-in message.
/// </summary>
public class ClockInChannel(long configId, ulong kookId)
{
    public long Id { get; set; }
    public long ConfigId { get; init; } = configId;
    public ulong KookId { get; init; } = kookId;
    public Guid? MessageId { get; set; }

    // References
    public ClockInConfig Config { get; set; } = null!;
}
