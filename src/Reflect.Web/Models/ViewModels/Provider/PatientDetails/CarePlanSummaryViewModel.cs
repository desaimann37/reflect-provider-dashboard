using Reflect.Domain.Clinical;
using Reflect.Domain.Common;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.PatientDetails;

/// <summary>Read-only view of the care plan. Editing happens on the separate Edit page.</summary>
public class CarePlanSummaryViewModel
{
    public string Program { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string ConsentDate { get; set; } = string.Empty;
    public string MonitoringStart { get; set; } = string.Empty;
    public string SetupCompleted { get; set; } = string.Empty;
    public string TargetSessions { get; set; } = string.Empty;
    public string ReviewCadence { get; set; } = string.Empty;
    public string Goals { get; set; } = string.Empty;
    public BadgeViewModel Status { get; set; }
    public string AppAccount { get; set; } = string.Empty;
    public string ConsentScope { get; set; } = string.Empty;
    public string LastModified { get; set; } = string.Empty;

    public static CarePlanSummaryViewModel FromEntity(CarePlan plan, PatientAppLink link) => new()
    {
        Program = plan.ProgramName,
        Diagnosis = $"{plan.PrimaryDiagnosisCode} · {plan.PrimaryDiagnosisDescription}",
        ConsentDate = plan.ConsentDate.ToUsDateString(),
        MonitoringStart = plan.MonitoringStartDate.ToUsDateString(),
        SetupCompleted = plan.SetupCompletedOn?.ToUsDateString() ?? "Not documented",
        TargetSessions = $"{plan.TargetSessionsPerWeek} sessions per week",
        ReviewCadence = $"Every {plan.ReviewCadenceDays} days",
        Goals = plan.Goals,
        Status = ProviderUi.CarePlanStatus(plan.Status),
        AppAccount = link == null ? "Not linked" : $"{ProviderUi.MaskAppKey(link.AppUserKey)} (linked {link.LinkedAt.ToUsDateString()})",
        ConsentScope = link?.ConsentScope ?? string.Empty,
        LastModified = plan.DateModified?.ToUsDateTimeString() ?? plan.DateCreated.ToUsDateString()
    };
}
