using Reflect.Domain.Common;
using Reflect.Domain.Rules;
using static Reflect.Domain.Tests.TestData;

namespace Reflect.Domain.Tests;

public class MeasureAndStatsTests
{
    [Theory]
    [InlineData(0, SeverityBand.Minimal)]
    [InlineData(4, SeverityBand.Minimal)]
    [InlineData(5, SeverityBand.Mild)]
    [InlineData(10, SeverityBand.Moderate)]
    [InlineData(15, SeverityBand.ModeratelySevere)]
    [InlineData(20, SeverityBand.Severe)]
    [InlineData(27, SeverityBand.Severe)]
    public void Phq9_bands(int score, SeverityBand expected) => Assert.Equal(expected, MeasureSeverity.Phq9(score));

    [Theory]
    [InlineData(4, SeverityBand.Minimal)]
    [InlineData(9, SeverityBand.Mild)]
    [InlineData(14, SeverityBand.Moderate)]
    [InlineData(15, SeverityBand.Severe)]
    public void Gad7_bands(int score, SeverityBand expected) => Assert.Equal(expected, MeasureSeverity.Gad7(score));

    [Theory]
    [InlineData("2026-10-12", "2026-10-12")]
    [InlineData("2026-10-16", "2026-10-12")]
    [InlineData("2026-10-18", "2026-10-12")]
    public void Weeks_start_on_monday(string day, string expected) =>
        Assert.Equal(DateOnly.Parse(expected), EngagementStats.WeekStart(DateOnly.Parse(day)));

    [Fact]
    public void Weekly_buckets_split_by_platform_and_include_empty_weeks()
    {
        var today = new DateOnly(2026, 10, 16);
        var sessions = new[]
        {
            Session(today, 30, ReflectPlatform.VirtualReality),
            Session(today.AddDays(-1), 10, ReflectPlatform.Mobile),
            Session(today.AddDays(-21), 12, ReflectPlatform.Mobile),
            Session(today.AddDays(-200), 12, ReflectPlatform.Mobile)
        };

        var weeks = EngagementStats.Weekly(sessions, today, 4);

        Assert.Equal(4, weeks.Count);
        Assert.Equal(new DateOnly(2026, 9, 21), weeks[0].WeekStart);
        Assert.Equal(1, weeks[0].MobileSessions);
        Assert.Equal(0, weeks[1].TotalSessions);
        Assert.Equal(1, weeks[3].VrSessions);
        Assert.Equal(40, weeks[3].TotalMinutes);
    }

    [Theory]
    [InlineData(0.5, EngagementTier.Low)]
    [InlineData(1, EngagementTier.Moderate)]
    [InlineData(2.9, EngagementTier.Moderate)]
    [InlineData(3, EngagementTier.High)]
    public void Engagement_tiers(double weekly, EngagementTier expected) => Assert.Equal(expected, OutcomesCalculator.TierFor(weekly));
}
