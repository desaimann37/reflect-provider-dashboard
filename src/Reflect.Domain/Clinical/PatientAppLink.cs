using Reflect.Domain.Common;

namespace Reflect.Domain.Clinical;

/// <summary>
/// The only bridge between product analytics and the clinical record. It maps a patient to the
/// pseudonymous key their Reflect apps report under, and records the patient's consent to share
/// app data with their care team.
/// </summary>
public class PatientAppLink : BaseEntity
{
    public Guid PatientId { get; set; }
    public string AppUserKey { get; set; } = string.Empty;
    public DateTimeOffset LinkedAt { get; set; }
    public DateTimeOffset ConsentedAt { get; set; }
    public string ConsentScope { get; set; } = string.Empty;
}
