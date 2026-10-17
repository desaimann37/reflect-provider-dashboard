using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Reflect.Domain.Clinical;
using Reflect.Domain.Common;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.EditCarePlan;

public class EditCarePlanViewModel
{
    [Required]
    public Guid PatientId { get; set; }

    [Required(ErrorMessage = "Choose a program.")]
    [Display(Name = "Program")]
    public string ProgramName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose a primary diagnosis.")]
    [Display(Name = "Primary diagnosis (ICD-10-CM)")]
    public string PrimaryDiagnosisCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the consent date.")]
    [Display(Name = "Consent date")]
    public string ConsentDate { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the monitoring start date.")]
    [Display(Name = "Monitoring start date")]
    public string MonitoringStartDate { get; set; } = string.Empty;

    [Display(Name = "Setup and education completed (CPT 98975)")]
    public string SetupCompletedOn { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter a weekly session goal.")]
    [Range(1, 14, ErrorMessage = "Choose between 1 and 14 sessions a week.")]
    [Display(Name = "Target sessions per week")]
    public int? TargetSessionsPerWeek { get; set; }

    [Required(ErrorMessage = "Choose a review cadence.")]
    [Display(Name = "Data review cadence")]
    public int? ReviewCadenceDays { get; set; }

    [Required(ErrorMessage = "Enter the care plan goals.")]
    [StringLength(1000, ErrorMessage = "Goals can be up to 1,000 characters.")]
    [Display(Name = "Goals")]
    public string Goals { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Status")]
    public CarePlanStatus Status { get; set; } = CarePlanStatus.Active;

    public string PatientName { get; set; } = string.Empty;
    public string Mrn { get; set; } = string.Empty;
    public string MaxDate { get; set; } = string.Empty;

    public List<SelectListItem> ProgramOptions => DiagnosisCatalog.Programs
        .Select(p => new SelectListItem(p, p, p == ProgramName))
        .ToList();

    public List<SelectListItem> DiagnosisOptions => DiagnosisCatalog.All
        .Select(d => new SelectListItem($"{d.Code} · {d.Description}", d.Code, d.Code == PrimaryDiagnosisCode))
        .ToList();

    public List<SelectListItem> CadenceOptions => new[] { 7, 14, 30 }
        .Select(d => new SelectListItem($"Every {d} days", d.ToString(System.Globalization.CultureInfo.InvariantCulture), ReviewCadenceDays == d))
        .ToList();

    public List<SelectListItem> StatusOptions => Enum.GetValues<CarePlanStatus>()
        .Select(s => new SelectListItem(ProviderUi.CarePlanStatus(s).Text, s.ToString(), Status == s))
        .ToList();

    public async Task PrepareData(ProviderRepositories repos, ICurrentUserService currentUser, Guid patientId, DateTimeOffset now, bool loadValues, CancellationToken ct)
    {
        var provider = await CaseloadDataLoader.GetCurrentProviderAsync(repos, currentUser, ct);
        var patient = await repos.Patients.GetForProviderAsync(patientId, provider.Id, ct)
            ?? throw new EntityNotFoundException($"Patient {patientId} was not found.");
        var plan = await repos.CarePlans.GetByPatientIdAsync(patient.Id, ct)
            ?? throw new EntityNotFoundException($"Care plan for patient {patientId} was not found.");

        PatientId = patient.Id;
        PatientName = patient.FullName;
        Mrn = patient.Mrn;
        MaxDate = now.ToEasternDate().ToUsDateString();

        if (loadValues)
        {
            ProgramName = plan.ProgramName;
            PrimaryDiagnosisCode = plan.PrimaryDiagnosisCode;
            ConsentDate = plan.ConsentDate.ToUsDateString();
            MonitoringStartDate = plan.MonitoringStartDate.ToUsDateString();
            SetupCompletedOn = plan.SetupCompletedOn?.ToUsDateString() ?? string.Empty;
            TargetSessionsPerWeek = plan.TargetSessionsPerWeek;
            ReviewCadenceDays = plan.ReviewCadenceDays;
            Goals = plan.Goals;
            Status = plan.Status;
        }
    }

    public async Task<EditCarePlanSaveData> PrepareDataForSave(ProviderRepositories repos, ICurrentUserService currentUser, DateTimeOffset now, ModelStateDictionary modelState, CancellationToken ct)
    {
        var provider = await CaseloadDataLoader.GetCurrentProviderAsync(repos, currentUser, ct);
        var patient = await repos.Patients.GetForProviderAsync(PatientId, provider.Id, ct)
            ?? throw new EntityNotFoundException($"Patient {PatientId} was not found.");
        var existing = await repos.CarePlans.GetByPatientIdAsync(patient.Id, ct)
            ?? throw new EntityNotFoundException($"Care plan for patient {PatientId} was not found.");
        var today = now.ToEasternDate();

        if (!DiagnosisCatalog.Programs.Contains(ProgramName))
        {
            modelState.AddModelError(nameof(ProgramName), "Choose a program from the list.");
        }
        var diagnosis = DiagnosisCatalog.Find(PrimaryDiagnosisCode);
        if (diagnosis == null)
        {
            modelState.AddModelError(nameof(PrimaryDiagnosisCode), "Choose a diagnosis from the list.");
        }
        if (ReviewCadenceDays is not (7 or 14 or 30))
        {
            modelState.AddModelError(nameof(ReviewCadenceDays), "Choose a review cadence from the list.");
        }

        var consentOk = FormDates.TryParse(ConsentDate, out var consent);
        var startOk = FormDates.TryParse(MonitoringStartDate, out var start);
        DateOnly? setup = null;
        if (!consentOk) modelState.AddModelError(nameof(ConsentDate), "Enter the date as M/d/yyyy.");
        if (!startOk) modelState.AddModelError(nameof(MonitoringStartDate), "Enter the date as M/d/yyyy.");
        if (!string.IsNullOrWhiteSpace(SetupCompletedOn))
        {
            if (FormDates.TryParse(SetupCompletedOn, out var parsedSetup)) setup = parsedSetup;
            else modelState.AddModelError(nameof(SetupCompletedOn), "Enter the date as M/d/yyyy, or leave it blank.");
        }
        if (consentOk && consent > today) modelState.AddModelError(nameof(ConsentDate), "The consent date can't be in the future.");
        if (startOk && consentOk && start < consent) modelState.AddModelError(nameof(MonitoringStartDate), "Monitoring can't start before consent.");
        if (startOk && start > today.AddDays(30)) modelState.AddModelError(nameof(MonitoringStartDate), "Choose a start date within the next 30 days.");
        if (setup.HasValue && consentOk && setup.Value < consent) modelState.AddModelError(nameof(SetupCompletedOn), "Setup can't be before consent.");
        if (setup.HasValue && setup.Value > today) modelState.AddModelError(nameof(SetupCompletedOn), "The setup date can't be in the future.");

        if (!modelState.IsValid)
        {
            return null;
        }

        return new EditCarePlanSaveData
        {
            PatientName = patient.FullName,
            CarePlan = new CarePlan
            {
                Id = existing.Id,
                DateCreated = existing.DateCreated,
                PatientId = patient.Id,
                ProgramName = ProgramName,
                PrimaryDiagnosisCode = diagnosis.Code,
                PrimaryDiagnosisDescription = diagnosis.Description,
                ConsentDate = consent,
                MonitoringStartDate = start,
                SetupCompletedOn = setup,
                TargetSessionsPerWeek = TargetSessionsPerWeek ?? existing.TargetSessionsPerWeek,
                ReviewCadenceDays = ReviewCadenceDays ?? existing.ReviewCadenceDays,
                Goals = Goals.Trim(),
                Status = Status,
                DateModified = now
            }
        };
    }
}

public sealed class EditCarePlanSaveData
{
    public CarePlan CarePlan { get; init; }
    public string PatientName { get; init; } = string.Empty;
}
