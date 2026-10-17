using Reflect.Domain.Analytics;
using Reflect.Domain.Common;
using Reflect.Domain.Rules;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.PatientDetails;

/// <summary>Weekly engagement for one patient. Rendered inline on first load and returned as JSON when the range changes.</summary>
public class EngagementChartViewModel
{
    public static readonly int[] AllowedWeeks = { 4, 12, 26 };

    public int Weeks { get; set; }
    public List<string> Labels { get; set; } = new();
    public List<double> VrMinutes { get; set; } = new();
    public List<double> MobileMinutes { get; set; } = new();
    public List<int> VrSessions { get; set; } = new();
    public List<int> MobileSessions { get; set; } = new();
    public string Summary { get; set; } = string.Empty;

    public async Task PrepareData(ProviderRepositories repos, ICurrentUserService currentUser, Guid patientId, int weeks, DateTimeOffset now, RtmRules rules, CancellationToken ct)
    {
        var provider = await CaseloadDataLoader.GetCurrentProviderAsync(repos, currentUser, ct);
        var summary = await CaseloadDataLoader.LoadPatientAsync(repos, provider, patientId, now, rules, ct);
        var built = Build(summary.Sessions, summary.Today, AllowedWeeks.Contains(weeks) ? weeks : 12);
        Weeks = built.Weeks;
        Labels = built.Labels;
        VrMinutes = built.VrMinutes;
        MobileMinutes = built.MobileMinutes;
        VrSessions = built.VrSessions;
        MobileSessions = built.MobileSessions;
        Summary = built.Summary;
    }

    public static EngagementChartViewModel Build(IEnumerable<EngagementSession> sessions, DateOnly today, int weeks)
    {
        var buckets = EngagementStats.Weekly(sessions, today, weeks);
        var totalVr = buckets.Sum(b => b.VrMinutes);
        var totalMobile = buckets.Sum(b => b.MobileMinutes);
        return new EngagementChartViewModel
        {
            Weeks = weeks,
            Labels = buckets.Select(b => ProviderUi.ShortDate(b.WeekStart)).ToList(),
            VrMinutes = buckets.Select(b => b.VrMinutes).ToList(),
            MobileMinutes = buckets.Select(b => b.MobileMinutes).ToList(),
            VrSessions = buckets.Select(b => b.VrSessions).ToList(),
            MobileSessions = buckets.Select(b => b.MobileSessions).ToList(),
            Summary = $"Weekly minutes over the last {weeks} weeks: {totalVr:0} in VR and {totalMobile:0} on mobile ({buckets.Sum(b => b.TotalSessions)} sessions)."
        };
    }
}
