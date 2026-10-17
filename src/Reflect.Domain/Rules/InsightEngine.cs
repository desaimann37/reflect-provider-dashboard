using Reflect.Domain.Clinical;
using Reflect.Domain.Common;

namespace Reflect.Domain.Rules;

/// <summary>
/// Deterministic pattern detection over one patient's engagement and therapeutic-response data.
/// Every rule is explainable and documented in docs/ARCHITECTURE.md. These are prompts for
/// clinical review, not diagnoses, and the dashboard is not a real-time crisis-monitoring tool.
/// </summary>
public static class InsightEngine
{
    public const int InactivityMediumDays = 7;
    public const int InactivityHighDays = 14;
    public const decimal MoodDeclineThreshold = 1.5m;
    public const decimal MoodLiftThreshold = 1.0m;

    public static IReadOnlyList<ClinicalInsight> Evaluate(InsightInput input)
    {
        var insights = new List<ClinicalInsight>();
        AddMeasureInsights(input, insights);
        if (input.IsMonitoringActive)
        {
            AddEngagementInsights(input, insights);
            AddMoodInsights(input, insights);
            AddRtmInsights(input, insights);
        }
        return insights.OrderBy(i => i.Severity).ThenByDescending(i => i.ObservedAt).ToList();
    }

    private static void AddMeasureInsights(InsightInput input, List<ClinicalInsight> insights)
    {
        var recentCutoff = input.Today.AddDays(-30);

        var item9 = Latest(input.Observations, ObservationCodes.Phq9Item9);
        if (item9 != null && item9.Value >= 1 && item9.EffectiveAt.ToEasternDate() >= recentCutoff)
        {
            insights.Add(new ClinicalInsight(
                InsightKind.SafetyScreenPositive,
                InsightSeverity.Critical,
                "PHQ-9 item 9 positive",
                $"Answered \"{MeasureSeverity.Item9Label(item9.Value)}\" on {item9.EffectiveAt.ToUsDateString()}. Review promptly and follow your facility's safety protocol.",
                item9.EffectiveAt));
        }

        var phq = Series(input.Observations, ObservationCodes.Phq9Total);
        if (phq.Count >= 2)
        {
            var latest = phq[^1];
            var previous = phq[^2];
            var change = latest.Value - previous.Value;
            if (change >= MeasureSeverity.MeaningfulChange)
            {
                insights.Add(new ClinicalInsight(
                    InsightKind.Phq9Worsening,
                    InsightSeverity.High,
                    $"PHQ-9 up {change:0} points",
                    $"From {previous.Value:0} ({MeasureSeverity.Label(MeasureSeverity.Phq9(previous.Value))}) to {latest.Value:0} ({MeasureSeverity.Label(MeasureSeverity.Phq9(latest.Value))}) since {previous.EffectiveAt.ToUsDateString()}.",
                    latest.EffectiveAt));
            }

            var baseline = phq[0];
            var improvement = baseline.Value - latest.Value;
            if (improvement >= MeasureSeverity.MeaningfulChange && change < MeasureSeverity.MeaningfulChange)
            {
                insights.Add(new ClinicalInsight(
                    InsightKind.Phq9Improving,
                    InsightSeverity.Positive,
                    $"PHQ-9 improved {improvement:0} points",
                    $"From {baseline.Value:0} at baseline ({baseline.EffectiveAt.ToUsDateString()}) to {latest.Value:0} ({MeasureSeverity.Label(MeasureSeverity.Phq9(latest.Value))}).",
                    latest.EffectiveAt));
            }
        }

        var gad = Series(input.Observations, ObservationCodes.Gad7Total);
        if (gad.Count >= 2)
        {
            var change = gad[^1].Value - gad[^2].Value;
            if (change >= MeasureSeverity.MeaningfulChange)
            {
                insights.Add(new ClinicalInsight(
                    InsightKind.Gad7Worsening,
                    InsightSeverity.Medium,
                    $"GAD-7 up {change:0} points",
                    $"From {gad[^2].Value:0} to {gad[^1].Value:0} ({MeasureSeverity.Label(MeasureSeverity.Gad7(gad[^1].Value))}) since {gad[^2].EffectiveAt.ToUsDateString()}.",
                    gad[^1].EffectiveAt));
            }
        }
    }

    private static void AddEngagementInsights(InsightInput input, List<ClinicalInsight> insights)
    {
        var sessions = input.Sessions;
        var daysEnrolled = input.Today.DayNumber - input.MonitoringStartDate.DayNumber;

        if (sessions.Count == 0)
        {
            if (daysEnrolled >= 3)
            {
                insights.Add(new ClinicalInsight(
                    InsightKind.NoDataSinceEnrollment,
                    InsightSeverity.Medium,
                    "No app data since enrollment",
                    $"Enrolled {daysEnrolled} days ago with no Reflect sessions. Check the app link and onboarding."));
            }
            return;
        }

        var last = sessions.Max(s => s.StartedAt);
        var idleDays = input.Today.DayNumber - last.ToEasternDate().DayNumber;
        if (idleDays >= InactivityMediumDays)
        {
            insights.Add(new ClinicalInsight(
                InsightKind.Inactivity,
                idleDays >= InactivityHighDays ? InsightSeverity.High : InsightSeverity.Medium,
                $"No Reflect activity in {idleDays} days",
                $"Last session was {last.ToUsDateString()}. Consider an outreach call.",
                last));
            return;
        }

        var lastWeek = EngagementStats.InLastDays(sessions, input.Today, 7).Count();
        var priorWindow = sessions.Count(s =>
        {
            var day = s.StartedAt.ToEasternDate();
            return day >= input.Today.AddDays(-34) && day <= input.Today.AddDays(-7);
        });
        var priorWeeklyAverage = priorWindow / 4.0;
        if (priorWeeklyAverage >= 2 && lastWeek < priorWeeklyAverage * 0.5)
        {
            var drop = (int)Math.Round((1 - lastWeek / priorWeeklyAverage) * 100);
            insights.Add(new ClinicalInsight(
                InsightKind.EngagementDrop,
                InsightSeverity.Medium,
                $"Engagement down {drop}% this week",
                $"{lastWeek} session{(lastWeek == 1 ? string.Empty : "s")} in the last 7 days against a recent average of {priorWeeklyAverage:0.#} per week."));
        }
    }

    private static void AddMoodInsights(InsightInput input, List<ClinicalInsight> insights)
    {
        var pre = input.Observations.Where(o => o.Code == ObservationCodes.MoodPre).ToList();
        var recent = pre.Where(o => o.EffectiveAt.ToEasternDate() > input.Today.AddDays(-14)).ToList();
        var prior = pre.Where(o =>
        {
            var day = o.EffectiveAt.ToEasternDate();
            return day <= input.Today.AddDays(-14) && day > input.Today.AddDays(-28);
        }).ToList();

        if (recent.Count >= 3 && prior.Count >= 3)
        {
            var drop = prior.Average(o => o.Value) - recent.Average(o => o.Value);
            if (drop >= MoodDeclineThreshold)
            {
                insights.Add(new ClinicalInsight(
                    InsightKind.MoodDecline,
                    InsightSeverity.Medium,
                    $"Pre-session mood down {drop:0.0} points",
                    $"Average check-in mood fell from {prior.Average(o => o.Value):0.0} to {recent.Average(o => o.Value):0.0} (out of 10) over the last two weeks."));
            }
        }

        var lift = AverageMoodLift(input.Observations, input.Today.AddDays(-29), input.Today, out var pairs);
        if (pairs >= 4 && lift >= MoodLiftThreshold)
        {
            insights.Add(new ClinicalInsight(
                InsightKind.MoodLift,
                InsightSeverity.Positive,
                $"Sessions lift mood by +{lift:0.0}",
                $"Average change from before to after a session across {pairs} check-ins in the last 30 days."));
        }
    }

    private static void AddRtmInsights(InsightInput input, List<ClinicalInsight> insights)
    {
        if (input.Rtm == null)
        {
            return;
        }
        if (input.Rtm.Readiness == RtmReadiness.AtRisk)
        {
            insights.Add(new ClinicalInsight(
                InsightKind.RtmAtRisk,
                InsightSeverity.Medium,
                "RTM period at risk",
                input.Rtm.Issues.Count > 0 ? input.Rtm.Issues[0] : "Thresholds may not be met this period."));
        }
        else if (input.Rtm.Readiness == RtmReadiness.ReadyToSign)
        {
            insights.Add(new ClinicalInsight(
                InsightKind.RtmReadyToSign,
                InsightSeverity.Low,
                "RTM period ready to sign",
                $"{input.Rtm.DataDays} data days and {input.Rtm.ManagementMinutes} management minutes. Document and sign before {input.Rtm.Period.End.ToUsDateString()}."));
        }
    }

    /// <summary>Average (post - pre) mood across sessions that have both check-ins.</summary>
    public static decimal AverageMoodLift(IEnumerable<Observation> observations, DateOnly from, DateOnly to, out int pairs)
    {
        var inRange = observations
            .Where(o => o.Code is ObservationCodes.MoodPre or ObservationCodes.MoodPost && !string.IsNullOrEmpty(o.SourceSessionId))
            .Where(o =>
            {
                var day = o.EffectiveAt.ToEasternDate();
                return day >= from && day <= to;
            })
            .GroupBy(o => o.SourceSessionId)
            .Select(g => (Pre: g.FirstOrDefault(o => o.Code == ObservationCodes.MoodPre), Post: g.FirstOrDefault(o => o.Code == ObservationCodes.MoodPost)))
            .Where(p => p.Pre != null && p.Post != null)
            .Select(p => p.Post.Value - p.Pre.Value)
            .ToList();

        pairs = inRange.Count;
        return pairs == 0 ? 0 : Math.Round(inRange.Average(), 1);
    }

    private static Observation Latest(IEnumerable<Observation> observations, string code) =>
        observations.Where(o => o.Code == code).OrderByDescending(o => o.EffectiveAt).FirstOrDefault();

    private static List<Observation> Series(IEnumerable<Observation> observations, string code) =>
        observations.Where(o => o.Code == code).OrderBy(o => o.EffectiveAt).ToList();
}
