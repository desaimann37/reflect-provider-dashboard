using Reflect.Domain.Analytics;
using Reflect.Domain.Clinical;
using Reflect.Domain.Rules;
using static Reflect.Domain.Tests.TestData;

namespace Reflect.Domain.Tests;

public class InsightEngineTests
{
    private static readonly DateOnly Today = new(2026, 10, 16);

    private static IReadOnlyList<ClinicalInsight> Run(IEnumerable<EngagementSession> sessions = null, IEnumerable<Observation> observations = null,
        DateOnly? start = null) =>
        InsightEngine.Evaluate(new InsightInput
        {
            Today = Today,
            MonitoringStartDate = start ?? Today.AddDays(-90),
            Sessions = (sessions ?? Array.Empty<EngagementSession>()).ToList(),
            Observations = (observations ?? Array.Empty<Observation>()).ToList()
        });

    private static IEnumerable<EngagementSession> Daily(int fromDaysAgo, int toDaysAgo) =>
        Enumerable.Range(toDaysAgo, fromDaysAgo - toDaysAgo + 1).Select(d => Session(Today.AddDays(-d)));

    [Fact]
    public void Positive_item_nine_is_critical_and_listed_first()
    {
        var obs = new[]
        {
            Obs(ObservationCodes.Phq9Total, 9, Today.AddDays(-16)),
            Obs(ObservationCodes.Phq9Total, 16, Today.AddDays(-2)),
            Obs(ObservationCodes.Phq9Item9, 1, Today.AddDays(-2))
        };

        var insights = Run(Daily(20, 0), obs);

        Assert.Equal(InsightKind.SafetyScreenPositive, insights[0].Kind);
        Assert.Equal(InsightSeverity.Critical, insights[0].Severity);
        Assert.Contains("Several days", insights[0].Detail);
        Assert.Contains(insights, i => i.Kind == InsightKind.Phq9Worsening && i.Severity == InsightSeverity.High);
    }

    [Fact]
    public void Old_positive_item_nine_is_not_raised()
    {
        var insights = Run(Daily(40, 0), new[] { Obs(ObservationCodes.Phq9Item9, 2, Today.AddDays(-45)) });

        Assert.DoesNotContain(insights, i => i.Kind == InsightKind.SafetyScreenPositive);
    }

    [Fact]
    public void Five_point_drop_from_baseline_is_a_positive_insight()
    {
        var obs = new[]
        {
            Obs(ObservationCodes.Phq9Total, 17, Today.AddDays(-60)),
            Obs(ObservationCodes.Phq9Total, 13, Today.AddDays(-30)),
            Obs(ObservationCodes.Phq9Total, 10, Today.AddDays(-3))
        };

        var insight = Assert.Single(Run(Daily(20, 0), obs), i => i.Kind == InsightKind.Phq9Improving);
        Assert.Equal(InsightSeverity.Positive, insight.Severity);
        Assert.Equal("PHQ-9 improved 7 points", insight.Title);
    }

    [Theory]
    [InlineData(9, InsightSeverity.Medium)]
    [InlineData(15, InsightSeverity.High)]
    public void Inactivity_severity_grows_with_idle_days(int idleDays, InsightSeverity expected)
    {
        var insight = Assert.Single(Run(Daily(40, idleDays)), i => i.Kind == InsightKind.Inactivity);

        Assert.Equal(expected, insight.Severity);
        Assert.Equal($"No Reflect activity in {idleDays} days", insight.Title);
    }

    [Fact]
    public void Engagement_drop_compares_last_week_with_prior_four_weeks()
    {
        var sessions = Daily(34, 7).Concat(new[] { Session(Today.AddDays(-2)) });

        var insight = Assert.Single(Run(sessions), i => i.Kind == InsightKind.EngagementDrop);
        Assert.Equal("Engagement down 86% this week", insight.Title);
    }

    [Fact]
    public void Steady_engagement_raises_no_engagement_flags()
    {
        var insights = Run(Daily(40, 0));

        Assert.DoesNotContain(insights, i => i.Kind is InsightKind.EngagementDrop or InsightKind.Inactivity);
    }

    [Fact]
    public void Mood_decline_and_mood_lift_are_detected_from_check_ins()
    {
        var obs = new List<Observation>();
        for (var d = 27; d >= 15; d -= 3) obs.Add(Obs(ObservationCodes.MoodPre, 7, Today.AddDays(-d)));
        for (var d = 12; d >= 0; d -= 3)
        {
            var id = $"s{d}";
            obs.Add(Obs(ObservationCodes.MoodPre, 5, Today.AddDays(-d), id));
            obs.Add(Obs(ObservationCodes.MoodPost, 6.5m, Today.AddDays(-d), id));
        }

        var insights = Run(Daily(30, 0), obs);

        Assert.Contains(insights, i => i.Kind == InsightKind.MoodDecline && i.Title == "Pre-session mood down 2.0 points");
        Assert.Contains(insights, i => i.Kind == InsightKind.MoodLift && i.Title == "Sessions lift mood by +1.5");
    }

    [Fact]
    public void Paused_monitoring_keeps_measure_flags_but_skips_engagement_flags()
    {
        var insights = InsightEngine.Evaluate(new InsightInput
        {
            Today = Today,
            MonitoringStartDate = Today.AddDays(-90),
            Sessions = Daily(60, 25).ToList(),
            Observations = new[] { Obs(ObservationCodes.Phq9Item9, 1, Today.AddDays(-3)) },
            IsMonitoringActive = false
        });

        Assert.Equal(InsightKind.SafetyScreenPositive, Assert.Single(insights).Kind);
    }

    [Fact]
    public void Enrolled_patient_without_data_is_flagged()
    {
        var insight = Assert.Single(Run(start: Today.AddDays(-6)));

        Assert.Equal(InsightKind.NoDataSinceEnrollment, insight.Kind);
    }
}
