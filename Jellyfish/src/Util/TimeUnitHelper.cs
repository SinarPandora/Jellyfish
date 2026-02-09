using Jellyfish.Core.Enum;

namespace Jellyfish.Util;

/// <summary>
///     Helper class for TimeUnit
/// </summary>
public static class TimeUnitHelper
{
    /// <summary>
    ///     Time unit + time value = time span
    /// </summary>
    /// <param name="unit">Time unit</param>
    /// <param name="value">Time value</param>
    /// <returns>Time span</returns>
    public static TimeSpan ToTimeSpan(this TimeUnit unit, uint value)
    {
        return unit switch
        {
            TimeUnit.Second => TimeSpan.FromSeconds(value),
            TimeUnit.Minute => TimeSpan.FromMinutes(value),
            TimeUnit.Hour => TimeSpan.FromHours(value),
            TimeUnit.Day => TimeSpan.FromDays(value),
            TimeUnit.Week => TimeSpan.FromDays(value * 7),
            TimeUnit.Month => TimeSpan.FromDays(value * 30),
            _ => throw new ArgumentOutOfRangeException(
                nameof(unit),
                unit,
                $"不支持的时间单位：{unit}"
            ),
        };
    }
}
