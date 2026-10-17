using Reflect.Domain.Rules;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.Caseload;

public class CaseloadRowViewModel
{
    public Guid PatientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string Mrn { get; set; } = string.Empty;
    public string AgeAndPronouns { get; set; } = string.Empty;
    public string Program { get; set; } = string.Empty;
    public string LastActivity { get; set; } = string.Empty;
    public long LastActivitySort { get; set; }
    public int Sessions30Days { get; set; }
    public double Minutes30Days { get; set; }
    public int VrSessions30Days { get; set; }
    public int MobileSessions30Days { get; set; }
    public int DataDays { get; set; }
    public int DataDaysPercent { get; set; }
    public BadgeViewModel Rtm { get; set; }
    public int RtmSort { get; set; }
    public List<BadgeViewModel> Flags { get; set; } = new();
    public int FlagSort { get; set; }

    public static CaseloadRowViewModel FromSummary(PatientMonitoringSummary summary, DateTimeOffset now)
    {
        var flags = summary.Insights.Where(i => i.NeedsAttention).ToList();
        return new CaseloadRowViewModel
        {
            PatientId = summary.Patient.Id,
            Name = summary.Patient.FullName,
            Initials = summary.Patient.Initials,
            Mrn = summary.Patient.Mrn,
            AgeAndPronouns = $"{summary.Patient.AgeOn(summary.Today)} · {summary.Patient.Pronouns}",
            Program = summary.CarePlan.ProgramName,
            LastActivity = ProviderUi.Relative(summary.LastActivityAt, now),
            LastActivitySort = summary.LastActivityAt?.ToUnixTimeSeconds() ?? 0,
            Sessions30Days = summary.Sessions30Days,
            Minutes30Days = summary.Minutes30Days,
            VrSessions30Days = summary.VrSessions30Days,
            MobileSessions30Days = summary.MobileSessions30Days,
            DataDays = summary.Rtm.DataDays,
            DataDaysPercent = Math.Min(100, summary.Rtm.DataDays * 100 / 16),
            Rtm = ProviderUi.Readiness(summary.Rtm.Readiness),
            RtmSort = (int)summary.Rtm.Readiness,
            Flags = flags.Take(2).Select(i =>
            {
                var severity = ProviderUi.Severity(i.Severity);
                return new BadgeViewModel(severity.Color, i.Title, ProviderUi.InsightIcon(i.Kind), i.Detail);
            }).ToList(),
            FlagSort = flags.Count == 0 ? 9 : (int)flags.Min(i => i.Severity)
        };
    }
}
