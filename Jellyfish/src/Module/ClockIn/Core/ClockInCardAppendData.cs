namespace Jellyfish.Module.ClockIn.Core;

/// <summary>
///     Today clock-in count and top3 username data for the clock-in card
/// </summary>
public record ClockInCardAppendData(int TodayClockInCount, string[] Top3Usernames);
