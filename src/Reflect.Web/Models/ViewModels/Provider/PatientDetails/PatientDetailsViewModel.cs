using System.Globalization;
using Reflect.Domain.Clinical;
using Reflect.Domain.Common;
using Reflect.Domain.Rules;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.PatientDetails;

public class PatientDetailsViewModel
{
    // Header
    public Guid PatientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string Pronouns { get; set; } = string.Empty;
    public string Mrn { get; set; } = string.Empty;
    public string AgeLabel { get; set; } = string.Empty;
    public string Program { get; set; } = string.Empty;
    public string DiagnosisCode { get; set; } = string.Empty;
    public string DiagnosisDescription { get; set; } = string.Empty;
    public string MonitoringSince { get; set; } = string.Empty;
    public BadgeViewModel RtmBadge { get; set; }
    public bool IsMonitoringActive { get; set; }

    // Who is viewing (for the PHI access log the controller writes)
    public Guid ViewerUserId { get; set; }
    public string ViewerName { get; set; } = string.Empty;

    // Overview
    public int DataDays { get; set; }
    public int DataDaysPercent { get; set; }
    public int DaysLeft { get; set; }
    public int Sessions7Days { get; set; }
    public int TargetSessionsPerWeek { get; set; }
    public int Sessions30Days { get; set; }
    public double Minutes30Days { get; set; }
    public string MoodLiftLabel { get; set; } = string.Empty;
    public string LastActivity { get; set; } = string.Empty;
    public List<InsightViewModel> Insights { get; set; } = new();
    public string EngagementChartJson { get; set; } = "{}";
    public string ModuleChartJson { get; set; } = "{}";
    public string MoodChartJson { get; set; } = "{}";
    public string EngagementSummary { get; set; } = string.Empty;
    public string ModuleSummary { get; set; } = string.Empty;
    public string MoodSummary { get; set; } = string.Empty;

    // Sessions
    public List<SessionRowViewModel> Sessions { get; set; } = new();

    // Therapeutic response
    public string MeasuresChartJson { get; set; } = "{}";
    public string MeasuresSummary { get; set; } = string.Empty;
    public List<MeasureRowViewModel> Measures { get; set; } = new();
    public List<CheckInRowViewModel> CheckIns { get; set; } = new();

    // RTM
    public RtmPanelViewModel Rtm { get; set; } = new();

    // Notes, care plan, access log
    public List<NoteRowViewModel> Notes { get; set; } = new();
    public CarePlanSummaryViewModel CarePlan { get; set; } = new();
    public List<AccessLogRowViewModel> AccessLog { get; set; } = new();

    public async Task PrepareData(ProviderRepositories repos, ICurrentUserService currentUser, Guid patientId, DateTimeOffset now, RtmRules rules, CancellationToken ct)
    {
        var provider = await CaseloadDataLoader.GetCurrentProviderAsync(repos, currentUser, ct);
        var summary = await CaseloadDataLoader.LoadPatientAsync(repos, provider, patientId, now, rules, ct);
        var patient = summary.Patient;
        var plan = summary.CarePlan;
        var today = summary.Today;

        PatientId = patient.Id;
        Name = patient.FullName;
        Initials = patient.Initials;
        Pronouns = patient.Pronouns;
        Mrn = patient.Mrn;
        AgeLabel = $"{patient.AgeOn(today)} years";
        Program = plan.ProgramName;
        DiagnosisCode = plan.PrimaryDiagnosisCode;
        DiagnosisDescription = plan.PrimaryDiagnosisDescription;
        MonitoringSince = plan.MonitoringStartDate.ToUsDateString();
        RtmBadge = ProviderUi.Readiness(summary.Rtm.Readiness);
        IsMonitoringActive = summary.Rtm.Readiness != RtmReadiness.Inactive;
        ViewerUserId = provider.UserId;
        ViewerName = provider.DisplayName;

        DataDays = summary.Rtm.DataDays;
        DataDaysPercent = Math.Min(100, summary.Rtm.DataDays * 100 / rules.FullSupplyMinDays);
        DaysLeft = summary.Rtm.DaysLeft;
        Sessions7Days = summary.Sessions7Days;
        TargetSessionsPerWeek = plan.TargetSessionsPerWeek;
        Sessions30Days = summary.Sessions30Days;
        Minutes30Days = summary.Minutes30Days;
        MoodLiftLabel = summary.MoodLiftPairs30Days == 0
            ? "No check-ins"
            : string.Format(CultureInfo.InvariantCulture, "{0}{1:0.0}", summary.MoodLift30Days >= 0 ? "+" : string.Empty, summary.MoodLift30Days);
        LastActivity = ProviderUi.Relative(summary.LastActivityAt, now);
        Insights = summary.Insights.Select(InsightViewModel.FromInsight).ToList();

        var engagement = EngagementChartViewModel.Build(summary.Sessions, today, 12);
        EngagementChartJson = ProviderUi.ToJson(engagement);
        EngagementSummary = engagement.Summary;
        BuildModuleChart(summary, today);
        BuildMoodChart(summary, today);

        Sessions = summary.Sessions
            .Where(s => s.StartedAt.ToEasternDate() >= today.AddDays(-90))
            .OrderByDescending(s => s.StartedAt)
            .Select(s => SessionRowViewModel.FromEntity(s, summary.Observations))
            .ToList();

        BuildMeasures(summary);
        CheckIns = CheckInRowViewModel.FromObservations(summary.Observations, summary.Sessions).Take(30).ToList();

        Rtm = RtmPanelViewModel.FromSummary(summary, rules);

        var notes = await repos.Notes.GetByPatientIdAsync(patient.Id, ct);
        Notes = notes.Select(NoteRowViewModel.FromEntity).ToList();
        CarePlan = CarePlanSummaryViewModel.FromEntity(plan, summary.AppLink);
        var access = await repos.AccessLog.GetRecentByPatientIdAsync(patient.Id, 25, ct);
        AccessLog = access.Select(AccessLogRowViewModel.FromEntity).ToList();
    }

    /// <summary>Shows the access the controller just recorded at the top of the log.</summary>
    public void AddAccessRow(PhiAccessLog entry) => AccessLog.Insert(0, AccessLogRowViewModel.FromEntity(entry));

    private void BuildModuleChart(PatientMonitoringSummary summary, DateOnly today)
    {
        var mix = EngagementStats.ModuleMix(EngagementStats.InLastDays(summary.Sessions, today, 30)).ToList();
        ModuleChartJson = ProviderUi.ToJson(new
        {
            labels = mix.Select(m => m.ModuleName),
            platforms = mix.Select(m => m.Platform == ReflectPlatform.VirtualReality ? "VR" : "Mobile"),
            sessions = mix.Select(m => m.Sessions),
            minutes = mix.Select(m => m.Minutes)
        });
        ModuleSummary = mix.Count == 0
            ? "No sessions in the last 30 days."
            : "Sessions by module, last 30 days: " + string.Join("; ", mix.Select(m => $"{m.ModuleName} ({(m.Platform == ReflectPlatform.VirtualReality ? "VR" : "mobile")}) {m.Sessions}")) + ".";
    }

    private void BuildMoodChart(PatientMonitoringSummary summary, DateOnly today)
    {
        var firstWeek = EngagementStats.WeekStart(today).AddDays(-7 * 7);
        var mood = summary.Observations
            .Where(o => o.Code is ObservationCodes.MoodPre or ObservationCodes.MoodPost)
            .Select(o => (Week: EngagementStats.WeekStart(o.EffectiveAt.ToEasternDate()), o.Code, o.Value))
            .Where(x => x.Week >= firstWeek)
            .ToList();
        var weeks = Enumerable.Range(0, 8).Select(i => firstWeek.AddDays(7 * i)).ToList();

        decimal? Avg(DateOnly week, string code)
        {
            var values = mood.Where(m => m.Week == week && m.Code == code).Select(m => m.Value).ToList();
            return values.Count == 0 ? null : Math.Round(values.Average(), 1);
        }

        var pre = weeks.Select(w => Avg(w, ObservationCodes.MoodPre)).ToList();
        var post = weeks.Select(w => Avg(w, ObservationCodes.MoodPost)).ToList();
        MoodChartJson = ProviderUi.ToJson(new { labels = weeks.Select(ProviderUi.ShortDate), pre, post });
        var lastPre = pre.LastOrDefault(v => v.HasValue);
        var lastPost = post.LastOrDefault(v => v.HasValue);
        MoodSummary = lastPre.HasValue && lastPost.HasValue
            ? $"Weekly average mood check-ins over 8 weeks. Most recent week with data: {lastPre:0.0} before and {lastPost:0.0} after sessions (out of 10)."
            : "Not enough mood check-ins to chart.";
    }

    private void BuildMeasures(PatientMonitoringSummary summary)
    {
        var phq = summary.Observations.Where(o => o.Code == ObservationCodes.Phq9Total).OrderBy(o => o.EffectiveAt).ToList();
        var gad = summary.Observations.Where(o => o.Code == ObservationCodes.Gad7Total).OrderBy(o => o.EffectiveAt).ToList();
        var item9 = summary.Observations.Where(o => o.Code == ObservationCodes.Phq9Item9).ToList();

        var dates = phq.Select(o => o.EffectiveAt.ToEasternDate())
            .Concat(gad.Select(o => o.EffectiveAt.ToEasternDate()))
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        decimal? On(List<Observation> series, DateOnly day) =>
            series.FirstOrDefault(o => o.EffectiveAt.ToEasternDate() == day)?.Value;

        MeasuresChartJson = ProviderUi.ToJson(new
        {
            labels = dates.Select(d => d.ToUsDateString()),
            phq9 = dates.Select(d => On(phq, d)),
            gad7 = dates.Select(d => On(gad, d))
        });
        MeasuresSummary = phq.Count == 0
            ? "No questionnaires recorded yet."
            : $"PHQ-9 from {phq[0].Value:0} to {phq[^1].Value:0}; GAD-7 from {(gad.Count > 0 ? gad[0].Value.ToString("0", CultureInfo.InvariantCulture) : "n/a")} to {(gad.Count > 0 ? gad[^1].Value.ToString("0", CultureInfo.InvariantCulture) : "n/a")}, across {dates.Count} check-ins.";

        Measures = MeasureRowViewModel.FromSeries(phq, ObservationCodes.Phq9Total, item9)
            .Concat(MeasureRowViewModel.FromSeries(gad, ObservationCodes.Gad7Total, item9))
            .OrderByDescending(m => m.SortKey)
            .ThenBy(m => m.Measure)
            .ToList();
    }
}
