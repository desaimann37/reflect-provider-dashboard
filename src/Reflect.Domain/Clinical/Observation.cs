using Reflect.Domain.Common;

namespace Reflect.Domain.Clinical;

/// <summary>
/// A single measured or reported value, modelled on the FHIR Observation resource. Therapeutic
/// response data (mood check-ins, PHQ-9, GAD-7) are rows here today. Future Remote Physiologic
/// Monitoring data (heart rate, HRV, sleep) becomes new <see cref="Code"/> values with
/// <see cref="ObservationSource.Device"/>, with no schema change.
/// </summary>
public class Observation : BaseEntity
{
    public Guid PatientId { get; set; }
    public string CodeSystem { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Display { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public ObservationSource Source { get; set; }
    public ReflectPlatform? Platform { get; set; }

    /// <summary>The analytics session this entry was captured in, if any (a soft reference, not a foreign key).</summary>
    public string SourceSessionId { get; set; } = string.Empty;

    public DateTimeOffset EffectiveAt { get; set; }
}
