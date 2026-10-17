using Reflect.Domain.Common;
using Reflect.Domain.Rules;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.Dashboard;

public class ProviderDashboardViewModel
{
    public string Greeting { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderTitle { get; set; } = string.Empty;
    public string ProviderInitials { get; set; } = string.Empty;
    public string FacilityName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TodayLong { get; set; } = string.Empty;

    public int ActivePatients { get; set; }
    public int EngagedLast7Days { get; set; }
    public int NeedsAttention { get; set; }
    public int UrgentCount { get; set; }
    public int ReadyToSign { get; set; }
    public int AtRisk { get; set; }
    public int SessionsLast7Days { get; set; }

    public List<AttentionItemViewModel> Attention { get; set; } = new();
    public List<ClosingPeriodRowViewModel> ClosingSoon { get; set; } = new();
    public List<RecentSessionRowViewModel> RecentSessions { get; set; } = new();
    public string CaseloadChartJson { get; set; } = "{}";
    public string CaseloadChartSummary { get; set; } = string.Empty;

    public async Task PrepareData(ProviderRepositories repos, ICurrentUserService currentUser, DateTimeOffset now, RtmRules rules, CancellationToken ct)
    {
        var provider = await CaseloadDataLoader.GetCurrentProviderAsync(repos, currentUser, ct);
        var caseload = await CaseloadDataLoader.LoadCaseloadAsync(repos, provider, now, rules, ct);
        var active = caseload.Where(s => s.Rtm.Readiness != RtmReadiness.Inactive).ToList();
        var today = now.ToEasternDate();

        Greeting = ProviderUi.Greeting(provider);
        ProviderName = provider.DisplayName;
        ProviderTitle = provider.Title;
        ProviderInitials = provider.Initials;
        FacilityName = provider.FacilityName;
        Email = provider.Email;
        TodayLong = today.ToUsLongDateString();

        ActivePatients = active.Count;
        EngagedLast7Days = active.Count(s => s.Sessions7Days > 0);
        NeedsAttention = active.Count(s => s.NeedsAttention);
        UrgentCount = active.Count(s => s.TopSeverity is InsightSeverity.Critical or InsightSeverity.High);
        ReadyToSign = active.Count(s => s.Rtm.Readiness == RtmReadiness.ReadyToSign);
        AtRisk = active.Count(s => s.Rtm.Readiness == RtmReadiness.AtRisk);
        SessionsLast7Days = active.Sum(s => s.Sessions7Days);

        Attention = active
            .Select(s => (Summary: s, Flags: s.Insights.Where(i => i.NeedsAttention && i.Kind is not InsightKind.RtmAtRisk).ToList()))
            .Where(x => x.Flags.Count > 0)
            .OrderBy(x => x.Flags[0].Severity)
            .ThenBy(x => x.Summary.Patient.LastName)
            .Take(8)
            .Select(x => AttentionItemViewModel.FromInsights(x.Summary, x.Flags))
            .ToList();

        ClosingSoon = active
            .Where(s => s.Rtm.DaysLeft <= 7 && s.Rtm.Readiness != RtmReadiness.Signed)
            .OrderBy(s => s.Rtm.Period.End)
            .ThenBy(s => s.Patient.LastName)
            .Select(ClosingPeriodRowViewModel.FromSummary)
            .ToList();

        RecentSessions = caseload
            .SelectMany(s => s.Sessions.Select(session => (Summary: s, Session: session)))
            .OrderByDescending(x => x.Session.StartedAt)
            .Take(8)
            .Select(x => RecentSessionRowViewModel.FromEntity(x.Summary.Patient, x.Session, now))
            .ToList();

        var weeks = EngagementStats.Weekly(caseload.SelectMany(s => s.Sessions), today, 12);
        CaseloadChartJson = ProviderUi.ToJson(new
        {
            labels = weeks.Select(w => ProviderUi.ShortDate(w.WeekStart)),
            vrSessions = weeks.Select(w => w.VrSessions),
            mobileSessions = weeks.Select(w => w.MobileSessions)
        });
        var last = weeks[^1];
        CaseloadChartSummary = $"Weekly sessions across the caseload for the last 12 weeks. This week so far: {last.VrSessions} VR and {last.MobileSessions} mobile sessions.";
    }
}
