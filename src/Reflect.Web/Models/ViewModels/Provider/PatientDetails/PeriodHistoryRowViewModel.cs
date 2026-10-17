using Reflect.Domain.Clinical;
using Reflect.Domain.Common;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.PatientDetails;

public class PeriodHistoryRowViewModel
{
    public string Period { get; set; } = string.Empty;
    public int DataDays { get; set; }
    public int ManagementMinutes { get; set; }
    public string Codes { get; set; } = string.Empty;
    public BadgeViewModel Status { get; set; }
    public string SignedBy { get; set; } = string.Empty;

    public static PeriodHistoryRowViewModel FromEntity(RtmPeriodReview review) => new()
    {
        Period = $"{review.PeriodStart.ToUsDateString()} - {review.PeriodEnd.ToUsDateString()}",
        DataDays = review.DataDaysAtSignOff,
        ManagementMinutes = review.ManagementMinutesAtSignOff,
        Codes = string.IsNullOrEmpty(review.CodesAttested) ? "None" : review.CodesAttested,
        Status = ProviderUi.Documentation(review.Status),
        SignedBy = review.SignedAt.HasValue ? $"{review.SignedByName}, {review.SignedAt.Value.ToUsDateTimeString()}" : "Awaiting sign-off"
    };
}
