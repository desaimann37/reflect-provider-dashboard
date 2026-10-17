using Reflect.Domain.Clinical;
using Reflect.Domain.Common;

namespace Reflect.Domain.Rules;

public sealed record ModuleOutcome(string ModuleName, ReflectPlatform Platform, int Sessions, int CheckInPairs, decimal AveragePre, decimal AveragePost)
{
    public decimal AverageLift => Math.Round(AveragePost - AveragePre, 1);
}

public sealed record PlatformOutcome(ReflectPlatform Platform, int Sessions, double AverageMinutes, double CompletionRate, decimal AverageLift, int CheckInPairs);

public enum EngagementTier
{
    Low = 1,
    Moderate = 2,
    High = 3
}

public sealed record TierOutcome(EngagementTier Tier, int Patients, int PatientsWithRepeatMeasure, decimal AveragePhq9Change, int MeaningfulImprovers);

public sealed record CaseloadOutcomes
{
    public decimal AverageMoodLift { get; init; }
    public double ShareOfSessionsImproved { get; init; }
    public int CheckInPairs { get; init; }
    public int PatientsWithRepeatPhq9 { get; init; }
    public int PatientsMeaningfullyImproved { get; init; }
    public double MedianWeeklySessions { get; init; }
    public IReadOnlyList<ModuleOutcome> Modules { get; init; } = Array.Empty<ModuleOutcome>();
    public IReadOnlyList<PlatformOutcome> Platforms { get; init; } = Array.Empty<PlatformOutcome>();
    public IReadOnlyList<TierOutcome> Tiers { get; init; } = Array.Empty<TierOutcome>();
}

/// <summary>
/// Caseload-level outcome aggregates. They are correlational: they show where Reflect use and
/// improvement move together. They do not establish that Reflect caused the improvement.
/// </summary>
public static class OutcomesCalculator
{
    public static EngagementTier TierFor(double weeklySessions) => weeklySessions switch
    {
        < 1 => EngagementTier.Low,
        < 3 => EngagementTier.Moderate,
        _ => EngagementTier.High
    };

    public static CaseloadOutcomes Calculate(IReadOnlyList<PatientMonitoringSummary> caseload)
    {
        var sessions = caseload.SelectMany(p => p.Sessions).ToList();
        var observations = caseload.SelectMany(p => p.Observations).ToList();
        var sessionById = sessions
            .Where(s => !string.IsNullOrEmpty(s.SourceSessionId))
            .GroupBy(s => s.SourceSessionId)
            .ToDictionary(g => g.Key, g => g.First());

        var pairs = observations
            .Where(o => o.Code is ObservationCodes.MoodPre or ObservationCodes.MoodPost && sessionById.ContainsKey(o.SourceSessionId))
            .GroupBy(o => o.SourceSessionId)
            .Select(g => (Session: sessionById[g.Key], Pre: g.FirstOrDefault(o => o.Code == ObservationCodes.MoodPre), Post: g.FirstOrDefault(o => o.Code == ObservationCodes.MoodPost)))
            .Where(p => p.Pre != null && p.Post != null)
            .ToList();

        var modules = sessions
            .GroupBy(s => (s.ModuleName, s.Platform))
            .Select(g =>
            {
                var modulePairs = pairs.Where(p => p.Session.ModuleName == g.Key.ModuleName && p.Session.Platform == g.Key.Platform).ToList();
                return new ModuleOutcome(
                    g.Key.ModuleName,
                    g.Key.Platform,
                    g.Count(),
                    modulePairs.Count,
                    modulePairs.Count == 0 ? 0 : Math.Round(modulePairs.Average(p => p.Pre.Value), 1),
                    modulePairs.Count == 0 ? 0 : Math.Round(modulePairs.Average(p => p.Post.Value), 1));
            })
            .Where(m => m.CheckInPairs > 0)
            .OrderByDescending(m => m.AverageLift)
            .ToList();

        var platforms = sessions
            .GroupBy(s => s.Platform)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var platformPairs = pairs.Where(p => p.Session.Platform == g.Key).ToList();
                return new PlatformOutcome(
                    g.Key,
                    g.Count(),
                    Math.Round(g.Average(s => s.DurationMinutes), 1),
                    Math.Round(g.Count(s => s.Completed) / (double)g.Count(), 3),
                    platformPairs.Count == 0 ? 0 : Math.Round(platformPairs.Average(p => p.Post.Value - p.Pre.Value), 1),
                    platformPairs.Count);
            })
            .ToList();

        var perPatient = caseload.Select(p =>
        {
            var weeks = Math.Max(1.0, (p.Today.DayNumber - p.CarePlan.MonitoringStartDate.DayNumber + 1) / 7.0);
            var weekly = p.Sessions.Count / weeks;
            var phq = p.Observations.Where(o => o.Code == ObservationCodes.Phq9Total).OrderBy(o => o.EffectiveAt).ToList();
            decimal? change = phq.Count >= 2 ? phq[^1].Value - phq[0].Value : null;
            return (Weekly: weekly, Tier: TierFor(weekly), Change: change);
        }).ToList();

        var tiers = Enum.GetValues<EngagementTier>()
            .Select(tier =>
            {
                var members = perPatient.Where(p => p.Tier == tier).ToList();
                var withRepeat = members.Where(p => p.Change.HasValue).ToList();
                return new TierOutcome(
                    tier,
                    members.Count,
                    withRepeat.Count,
                    withRepeat.Count == 0 ? 0 : Math.Round(withRepeat.Average(p => p.Change.Value), 1),
                    withRepeat.Count(p => p.Change.Value <= -MeasureSeverity.MeaningfulChange));
            })
            .ToList();

        var weeklyValues = perPatient.Select(p => p.Weekly).OrderBy(v => v).ToList();
        var median = weeklyValues.Count == 0
            ? 0
            : weeklyValues.Count % 2 == 1
                ? weeklyValues[weeklyValues.Count / 2]
                : (weeklyValues[weeklyValues.Count / 2 - 1] + weeklyValues[weeklyValues.Count / 2]) / 2;

        return new CaseloadOutcomes
        {
            AverageMoodLift = pairs.Count == 0 ? 0 : Math.Round(pairs.Average(p => p.Post.Value - p.Pre.Value), 1),
            ShareOfSessionsImproved = pairs.Count == 0 ? 0 : Math.Round(pairs.Count(p => p.Post.Value > p.Pre.Value) / (double)pairs.Count, 3),
            CheckInPairs = pairs.Count,
            PatientsWithRepeatPhq9 = perPatient.Count(p => p.Change.HasValue),
            PatientsMeaningfullyImproved = perPatient.Count(p => p.Change.HasValue && p.Change.Value <= -MeasureSeverity.MeaningfulChange),
            MedianWeeklySessions = Math.Round(median, 1),
            Modules = modules,
            Platforms = platforms,
            Tiers = tiers
        };
    }
}
