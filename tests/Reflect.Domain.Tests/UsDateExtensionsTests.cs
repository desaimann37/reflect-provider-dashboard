using Reflect.Domain.Common;

namespace Reflect.Domain.Tests;

public class UsDateExtensionsTests
{
    [Fact]
    public void Late_evening_utc_instant_shows_previous_eastern_day()
    {
        var utc = new DateTimeOffset(2026, 10, 17, 3, 30, 0, TimeSpan.Zero);

        Assert.Equal("10/16/2026", utc.ToUsDateString());
        Assert.Equal("10/16/2026 11:30 PM", utc.ToUsDateTimeString());
        Assert.Equal(new DateOnly(2026, 10, 16), utc.ToEasternDate());
    }

    [Fact]
    public void Winter_dates_use_standard_time_offset()
    {
        var utc = new DateTimeOffset(2026, 1, 15, 14, 5, 0, TimeSpan.Zero);

        Assert.Equal("1/15/2026 9:05 AM", utc.ToUsDateTimeString());
    }

    [Theory]
    [InlineData(2026, 10, 16, 4)]
    [InlineData(2026, 1, 15, 5)]
    public void Start_of_eastern_day_respects_daylight_saving(int y, int m, int d, int expectedUtcHour)
    {
        var start = new DateOnly(y, m, d).StartOfEasternDayUtc();

        Assert.Equal(TimeSpan.Zero, start.Offset);
        Assert.Equal(expectedUtcHour, start.Hour);
        Assert.Equal(new DateOnly(y, m, d), start.ToEasternDate());
    }

    [Fact]
    public void DateOnly_formats_without_leading_zeros()
    {
        Assert.Equal("3/7/2026", new DateOnly(2026, 3, 7).ToUsDateString());
        Assert.Equal("Friday, October 16, 2026", new DateOnly(2026, 10, 16).ToUsLongDateString());
    }
}
