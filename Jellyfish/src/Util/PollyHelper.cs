using Polly.Retry;

namespace Jellyfish.Util;

/// <summary>
///     Common retry utils for polly
/// </summary>
public static class PollyHelper
{
    /// <summary>
    ///     Progressive delay generator
    ///     There is no delay at the beginning,
    ///     a one-second delay for the second time,
    ///     and a three-second delay for each call after that.
    /// </summary>
    public static readonly Func<RetryDelayGeneratorArguments<object>, ValueTask<TimeSpan?>> ProgressiveDelayGenerator =
        args =>
        {
            var delay = args.AttemptNumber switch
            {
                0 => TimeSpan.Zero,
                1 => TimeSpan.FromSeconds(1),
                _ => TimeSpan.FromSeconds(3)
            };

            return new ValueTask<TimeSpan?>(delay);
        };
}
