using Reflect.Domain.Common;

namespace Reflect.Domain.Analytics;

/// <summary>
/// One Reflect XR or Reflect Mobile session, built from product-analytics events
/// (Firebase Analytics exported to BigQuery). It carries no PHI: the only identifier is the
/// pseudonymous <see cref="AppUserKey"/>. Linking a key to a patient happens only in the clinical
/// context through <c>PatientAppLink</c>.
/// </summary>
public class EngagementSession : BaseEntity
{
    public string AppUserKey { get; set; } = string.Empty;
    public string SourceSessionId { get; set; } = string.Empty;
    public ReflectPlatform Platform { get; set; }
    public string ModuleKey { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public int DurationSeconds { get; set; }
    public bool Completed { get; set; }
    public int CreationsSaved { get; set; }

    public double DurationMinutes => DurationSeconds / 60.0;
}
