using Polly.Retry;

namespace Jellyfish.Util;

/// <summary>
///     Common retry utils for polly
/// </summary>
public static class PollyHelper
{
    /// <summary>
    ///     Default progressive delay generator
    ///     There is no delay at the beginning,
    ///     a one-second delay for the second time,
    ///     and a three-second delay for each call after that.
    /// </summary>
    public static readonly Func<RetryDelayGeneratorArguments<object>, ValueTask<TimeSpan?>>
        DefaultProgressiveDelayGenerator = ProgressiveDelayGenerator(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1));

    /// <summary>
    ///     "Progressive delay generator" generator
    /// </summary>
    /// <param name="start">Start duration</param>
    /// <param name="step">Duration increase in each step</param>
    /// <returns>progressive delay generator function</returns>
    public static Func<RetryDelayGeneratorArguments<object>, ValueTask<TimeSpan?>> ProgressiveDelayGenerator(
        TimeSpan start, TimeSpan step)
    {
        return args =>
        {
            var delay = args.AttemptNumber switch
            {
                0 => start,
                1 => start + step,
                _ => start + step * 2
            };

            return new ValueTask<TimeSpan?>(delay);
        };
    }
}
