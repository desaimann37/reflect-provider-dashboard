using Reflect.Domain.Rules;

namespace Reflect.Domain.Tests;

public class BriefComposerTests
{
    [Fact]
    public void Flags_lead_the_headline_and_become_topics()
    {
        var brief = RuleBasedBriefComposer.Compose(new BriefInput
        {
            TargetSessionsPerWeek = 3,
            Sessions7Days = 1,
            Sessions30Days = 9,
            Minutes30Days = 140,
            VrSessions30Days = 4,
            MobileSessions30Days = 5,
            TopModules = new[] { "Creative Calm", "Reflect" },
            Phq9 = new[] { new MeasurePoint(30, 9, "Mild"), new MeasurePoint(2, 15, "Moderately severe") },
            Flags = new[] { "PHQ-9 up 6 points" }
        });

        Assert.Equal("rules", brief.Source);
        Assert.StartsWith("1 item to review: PHQ-9 up 6 points", brief.Headline);
        Assert.Contains(brief.Highlights, h => h.StartsWith("PHQ-9 is 15 (Moderately severe)"));
        Assert.Contains("Follow up on: PHQ-9 up 6 points.", brief.SuggestedTopics);
    }

    [Fact]
    public void No_flags_and_goal_met_reads_as_steady()
    {
        var brief = RuleBasedBriefComposer.Compose(new BriefInput { TargetSessionsPerWeek = 3, Sessions7Days = 4 });

        Assert.Equal("Steady engagement this week with no flags raised.", brief.Headline);
        Assert.NotEmpty(brief.SuggestedTopics);
    }
}
