using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Jellyfish.Module.ClockIn.Core;

/// <summary>
///     A buffer used to reduce the pressure of clock-in message updates
/// </summary>
public class ClockInBuffer
{
    /// <summary>
    ///     Buffer for (guildId, channelId, userId)
    /// </summary>
    public readonly Subject<(ulong, ulong, ulong)> Instance = new();

    public ClockInBuffer(UserClockInService service)
    {
        // Control the pressure to one operation per second
        Instance
            .Buffer(TimeSpan.FromSeconds(1))
            .SelectMany(ids => ids.Distinct())
            .Subscribe(pair => _ = service.ClockIn(pair.Item1, pair.Item2, pair.Item3));
    }
}
