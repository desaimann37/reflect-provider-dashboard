using Reflect.Domain.Common;

namespace Reflect.Domain.Clinical;

/// <summary>A patient's Remote Therapeutic Monitoring enrollment and treatment goals.</summary>
public class CarePlan : BaseEntity
{
    public Guid PatientId { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public string PrimaryDiagnosisCode { get; set; } = string.Empty;
    public string PrimaryDiagnosisDescription { get; set; } = string.Empty;
    public DateOnly ConsentDate { get; set; }
    public DateOnly MonitoringStartDate { get; set; }

    /// <summary>When RTM setup and patient education (CPT 98975) was completed, if it has been.</summary>
    public DateOnly? SetupCompletedOn { get; set; }

    public int TargetSessionsPerWeek { get; set; } = 3;
    public int ReviewCadenceDays { get; set; } = 7;
    public string Goals { get; set; } = string.Empty;
    public CarePlanStatus Status { get; set; } = CarePlanStatus.Active;
    public DateTimeOffset? DateModified { get; set; }
}
