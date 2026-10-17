using Reflect.Domain.Common;

namespace Reflect.Domain.Clinical;

/// <summary>A provider's documentation and sign-off for one 30-day RTM monitoring period.</summary>
public class RtmPeriodReview : BaseEntity
{
    public Guid PatientId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public DocumentationStatus Status { get; set; }
    public int DataDaysAtSignOff { get; set; }
    public int ManagementMinutesAtSignOff { get; set; }
    public string CodesAttested { get; set; } = string.Empty;
    public Guid? SignedByProviderId { get; set; }
    public string SignedByName { get; set; } = string.Empty;
    public DateTimeOffset? SignedAt { get; set; }
}
