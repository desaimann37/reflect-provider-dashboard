using Reflect.Domain.Common;
using Reflect.Domain.Rules;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.RtmWorklist;

public class RtmWorklistRowViewModel
{
    public Guid PatientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Mrn { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string PeriodEnd { get; set; } = string.Empty;
    public long PeriodEndSort { get; set; }
    public int DaysLeft { get; set; }
    public int DataDays { get; set; }
    public int DataDaysPercent { get; set; }
    public int ManagementMinutes { get; set; }
    public bool HasCall { get; set; }
    public List<string> Codes { get; set; } = new();
    public BadgeViewModel Readiness { get; set; }
    public int ReadinessSort { get; set; }
    public BadgeViewModel Documentation { get; set; }
    public string NextStep { get; set; } = string.Empty;

    public static RtmWorklistRowViewModel FromSummary(PatientMonitoringSummary summary)
    {
        var rtm = summary.Rtm;
        return new RtmWorklistRowViewModel
        {
            PatientId = summary.Patient.Id,
            Name = summary.Patient.FullName,
            Mrn = summary.Patient.Mrn,
            Period = ProviderUi.PeriodLabel(rtm.Period),
            PeriodEnd = rtm.Period.End.ToUsDateString(),
            PeriodEndSort = rtm.Period.End.DayNumber,
            DaysLeft = rtm.DaysLeft,
            DataDays = rtm.DataDays,
            DataDaysPercent = Math.Min(100, rtm.DataDays * 100 / 16),
            ManagementMinutes = rtm.ManagementMinutes,
            HasCall = rtm.HasInteractiveCommunication,
            Codes = rtm.EligibleCodes.Select(c => c.Label).ToList(),
            Readiness = ProviderUi.Readiness(rtm.Readiness),
            ReadinessSort = rtm.Readiness switch
            {
                RtmReadiness.ReadyToSign => 0,
                RtmReadiness.AtRisk => 1,
                RtmReadiness.OnTrack => 2,
                RtmReadiness.Signed => 3,
                _ => 4
            },
            Documentation = ProviderUi.Documentation(rtm.DocumentationStatus),
            NextStep = rtm.Readiness switch
            {
                RtmReadiness.ReadyToSign => "Review and sign",
                RtmReadiness.Signed => "Complete",
                RtmReadiness.Inactive => "Care plan paused",
                _ => rtm.Issues.FirstOrDefault() ?? "Keep monitoring"
            }
        };
    }
}
