using Reflect.Domain.Common;

namespace Reflect.Domain.Clinical;

/// <summary>
/// A record that someone viewed or used a patient's PHI. The real app audits writes automatically.
/// HIPAA also expects reads of PHI to be traceable, so the Provider area records them explicitly.
/// </summary>
public class PhiAccessLog : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid UserId { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
}
