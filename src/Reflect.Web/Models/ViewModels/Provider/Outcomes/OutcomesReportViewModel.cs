using System.Globalization;
using Reflect.Domain.Common;
using Reflect.Domain.Rules;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.Outcomes;

public class OutcomesReportViewModel
{
    public int Patients { get; set; }
    public string AverageMoodLift { get; set; } = string.Empty;
    public string ShareImproved { get; set; } = string.Empty;
    public int CheckInPairs { get; set; }
    public string MeaningfulImprovement { get; set; } = string.Empty;
    public string MedianWeeklySessions { get; set; } = string.Empty;
    public string ModuleChartJson { get; set; } = "{}";
    public string ModuleChartSummary { get; set; } = string.Empty;
    public string TierChartJson { get; set; } = "{}";
    public string TierChartSummary { get; set; } = string.Empty;
    public List<PlatformOutcomeRowViewModel> Platforms { get; set; } = new();
    public List<TierOutcomeRowViewModel> Tiers { get; set; } = new();

    public async Task PrepareData(ProviderRepositories repos, ICurrentUserService currentUser, DateTimeOffset now, RtmRules rules, CancellationToken ct)
    {
        var provider = await CaseloadDataLoader.GetCurrentProviderAsync(repos, currentUser, ct);
        var caseload = await CaseloadDataLoader.LoadCaseloadAsync(repos, provider, now, rules, ct);
        var outcomes = OutcomesCalculator.Calculate(caseload);

        Patients = caseload.Count;
        AverageMoodLift = string.Format(CultureInfo.InvariantCulture, "{0}{1:0.0}", outcomes.AverageMoodLift >= 0 ? "+" : string.Empty, outcomes.AverageMoodLift);
        ShareImproved = Percent(outcomes.ShareOfSessionsImproved);
        CheckInPairs = outcomes.CheckInPairs;
        MeaningfulImprovement = $"{outcomes.PatientsMeaningfullyImproved} of {outcomes.PatientsWithRepeatPhq9}";
        MedianWeeklySessions = outcomes.MedianWeeklySessions.ToString("0.0", CultureInfo.InvariantCulture);

        var modules = outcomes.Modules.Where(m => m.CheckInPairs >= 5).ToList();
        ModuleChartJson = ProviderUi.ToJson(new
        {
            labels = modules.Select(m => $"{m.ModuleName} ({(m.Platform == ReflectPlatform.VirtualReality ? "VR" : "Mobile")})"),
            platforms = modules.Select(m => m.Platform == ReflectPlatform.VirtualReality ? "VR" : "Mobile"),
            lift = modules.Select(m => m.AverageLift),
            pairs = modules.Select(m => m.CheckInPairs)
        });
        ModuleChartSummary = modules.Count == 0
            ? "Not enough check-ins to compare modules."
            : $"Average mood change from before to after a session, by module. Largest: {modules[0].ModuleName} at +{modules[0].AverageLift:0.0}; smallest: {modules[^1].ModuleName} at {modules[^1].AverageLift:+0.0;-0.0;0.0}.";

        TierChartJson = ProviderUi.ToJson(new
        {
            labels = outcomes.Tiers.Select(t => TierLabel(t.Tier)),
            change = outcomes.Tiers.Select(t => t.AveragePhq9Change),
            patients = outcomes.Tiers.Select(t => t.PatientsWithRepeatMeasure)
        });
        TierChartSummary = "Average PHQ-9 change from baseline by engagement level (negative is improvement): " +
            string.Join("; ", outcomes.Tiers.Select(t => $"{TierLabel(t.Tier)} {t.AveragePhq9Change:+0.0;-0.0;0.0} (n={t.PatientsWithRepeatMeasure})")) + ".";

        Platforms = outcomes.Platforms.Select(PlatformOutcomeRowViewModel.FromOutcome).ToList();
        Tiers = outcomes.Tiers.Select(TierOutcomeRowViewModel.FromOutcome).ToList();
    }

    public static string Percent(double share) =>
        (share * 100).ToString("0", CultureInfo.InvariantCulture) + "%";

    public static string TierLabel(EngagementTier tier) => tier switch
    {
        EngagementTier.Low => "Low (under 1 a week)",
        EngagementTier.Moderate => "Moderate (1-3 a week)",
        _ => "High (3+ a week)"
    };
}

public class PlatformOutcomeRowViewModel
{
    public BadgeViewModel Platform { get; set; }
    public int Sessions { get; set; }
    public string AverageMinutes { get; set; } = string.Empty;
    public string CompletionRate { get; set; } = string.Empty;
    public string AverageLift { get; set; } = string.Empty;
    public int CheckInPairs { get; set; }

    public static PlatformOutcomeRowViewModel FromOutcome(PlatformOutcome outcome) => new()
    {
        Platform = ProviderUi.Platform(outcome.Platform),
        Sessions = outcome.Sessions,
        AverageMinutes = ProviderUi.Minutes(outcome.AverageMinutes),
        CompletionRate = OutcomesReportViewModel.Percent(outcome.CompletionRate),
        AverageLift = outcome.AverageLift.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture),
        CheckInPairs = outcome.CheckInPairs
    };
}

public class TierOutcomeRowViewModel
{
    public string Tier { get; set; } = string.Empty;
    public int Patients { get; set; }
    public int WithRepeatMeasure { get; set; }
    public string AverageChange { get; set; } = string.Empty;
    public int MeaningfulImprovers { get; set; }

    public static TierOutcomeRowViewModel FromOutcome(TierOutcome outcome) => new()
    {
        Tier = OutcomesReportViewModel.TierLabel(outcome.Tier),
        Patients = outcome.Patients,
        WithRepeatMeasure = outcome.PatientsWithRepeatMeasure,
        AverageChange = outcome.PatientsWithRepeatMeasure == 0 ? "n/a" : outcome.AveragePhq9Change.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture),
        MeaningfulImprovers = outcome.MeaningfulImprovers
    };
}
