using Reflect.Domain.Common;
using Reflect.Domain.Rules;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.Dashboard;

public class ClosingPeriodRowViewModel
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PeriodEnd { get; set; } = string.Empty;
    public int DaysLeft { get; set; }
    public string DataDays { get; set; } = string.Empty;
    public int ManagementMinutes { get; set; }
    public BadgeViewModel Readiness { get; set; }

    public static ClosingPeriodRowViewModel FromSummary(PatientMonitoringSummary summary) => new()
    {
        PatientId = summary.Patient.Id,
        PatientName = summary.Patient.FullName,
        PeriodEnd = summary.Rtm.Period.End.ToUsDateString(),
        DaysLeft = summary.Rtm.DaysLeft,
        DataDays = $"{summary.Rtm.DataDays} / 16",
        ManagementMinutes = summary.Rtm.ManagementMinutes,
        Readiness = ProviderUi.Readiness(summary.Rtm.Readiness)
    };
}
