using System.Globalization;

namespace Reflect.Domain.Rules;

/// <summary>Writes a pre-session brief from de-identified facts without any external service.</summary>
public static class RuleBasedBriefComposer
{
    public static ClinicalBrief Compose(BriefInput input)
    {
        var highlights = new List<string>();
        var topics = new List<string>();

        var engagement = input.Sessions7Days >= input.TargetSessionsPerWeek ? "meeting" : "below";
        highlights.Add($"{input.Sessions7Days} session{S(input.Sessions7Days)} in the last 7 days ({engagement} the goal of {input.TargetSessionsPerWeek}); {input.Sessions30Days} sessions and {input.Minutes30Days.ToString("0", CultureInfo.InvariantCulture)} minutes in the last 30 days.");

        if (input.Sessions30Days > 0)
        {
            highlights.Add($"Platform mix (30 days): {input.VrSessions30Days} VR and {input.MobileSessions30Days} mobile session{S(input.MobileSessions30Days)}. Most used: {string.Join(", ", input.TopModules.Take(3))}.");
        }
        if (input.DaysSinceLastSession is >= 7)
        {
            topics.Add("Explore what has made it harder to use Reflect recently.");
        }

        if (input.MoodCheckIns30Days >= 4)
        {
            var direction = input.MoodLift30Days >= 0 ? "+" : string.Empty;
            highlights.Add($"Mood check-ins change by {direction}{input.MoodLift30Days.ToString("0.0", CultureInfo.InvariantCulture)} points on average from before to after a session ({input.MoodCheckIns30Days} check-ins).");
            if (input.MoodLift30Days >= 1)
            {
                topics.Add("Ask which activities felt most helpful and how to use them between sessions.");
            }
        }

        if (input.Phq9.Count > 0)
        {
            var latest = input.Phq9[^1];
            var trend = input.Phq9.Count >= 2 ? $" (was {input.Phq9[0].Score:0} {input.Phq9[0].DaysAgo} days ago)" : string.Empty;
            highlights.Add($"PHQ-9 is {latest.Score:0} ({latest.Severity}) as of {latest.DaysAgo} day{S(latest.DaysAgo)} ago{trend}.");
        }
        if (input.Gad7.Count > 0)
        {
            var latest = input.Gad7[^1];
            highlights.Add($"GAD-7 is {latest.Score:0} ({latest.Severity}) as of {latest.DaysAgo} day{S(latest.DaysAgo)} ago.");
        }

        foreach (var flag in input.Flags.Take(3))
        {
            topics.Add($"Follow up on: {flag}.");
        }
        if (!string.IsNullOrEmpty(input.RtmSummary))
        {
            highlights.Add(input.RtmSummary);
        }
        if (topics.Count == 0)
        {
            topics.Add("Review progress toward care-plan goals and reinforce current routine.");
        }

        var headline = input.Flags.Count > 0
            ? $"{input.Flags.Count} item{S(input.Flags.Count)} to review: {input.Flags[0]}."
            : input.Sessions7Days >= input.TargetSessionsPerWeek
                ? "Steady engagement this week with no flags raised."
                : "Engagement below goal this week; no clinical flags raised.";

        return new ClinicalBrief
        {
            Source = "rules",
            Headline = headline,
            Highlights = highlights,
            SuggestedTopics = topics.Distinct().Take(4).ToList(),
            Notice = "Composed from monitoring data by fixed rules. Verify against the record before acting."
        };
    }

    private static string S(int n) => n == 1 ? string.Empty : "s";
}
