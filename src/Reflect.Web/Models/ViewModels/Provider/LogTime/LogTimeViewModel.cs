using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Reflect.Domain.Clinical;
using Reflect.Domain.Common;
using Reflect.Domain.Rules;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.LogTime;

public class LogTimeViewModel
{
    [Required]
    public Guid PatientId { get; set; }

    [Required(ErrorMessage = "Enter the service date.")]
    [Display(Name = "Service date")]
    public string ServiceDate { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the minutes spent.")]
    [Range(1, 120, ErrorMessage = "Minutes must be between 1 and 120.")]
    [Display(Name = "Minutes")]
    public int? Minutes { get; set; }

    [Required(ErrorMessage = "Choose an activity.")]
    [Display(Name = "Activity")]
    public TimeActivity? Activity { get; set; }

    [Display(Name = "This included a real-time interactive communication with the patient or caregiver")]
    public bool IsInteractiveCommunication { get; set; }

    [Display(Name = "Communication method")]
    public CommunicationMethod CommunicationMethod { get; set; }

    [StringLength(1000, ErrorMessage = "Notes can be up to 1,000 characters.")]
    [Display(Name = "Notes")]
    public string Notes { get; set; } = string.Empty;

    // Display-only
    public string PatientName { get; set; } = string.Empty;
    public string Mrn { get; set; } = string.Empty;
    public string MonthLabel { get; set; } = string.Empty;
    public int MonthMinutes { get; set; }
    public bool MonthHasCall { get; set; }
    public string MaxDate { get; set; } = string.Empty;
    public string MinDate { get; set; } = string.Empty;

    public List<SelectListItem> ActivityOptions => Enum.GetValues<TimeActivity>()
        .Select(a => new SelectListItem(ProviderUi.ActivityLabel(a), a.ToString(), Activity == a))
        .ToList();

    public List<SelectListItem> MethodOptions => new[] { CommunicationMethod.Phone, CommunicationMethod.Video, CommunicationMethod.InPerson }
        .Select(m => new SelectListItem(ProviderUi.MethodLabel(m), m.ToString(), CommunicationMethod == m))
        .ToList();

    public async Task PrepareData(ProviderRepositories repos, ICurrentUserService currentUser, Guid patientId, DateTimeOffset now, RtmRules rules, CancellationToken ct)
    {
        var provider = await CaseloadDataLoader.GetCurrentProviderAsync(repos, currentUser, ct);
        var summary = await CaseloadDataLoader.LoadPatientAsync(repos, provider, patientId, now, rules, ct);
        var today = summary.Today;

        PatientId = summary.Patient.Id;
        PatientName = summary.Patient.FullName;
        Mrn = summary.Patient.Mrn;
        MonthLabel = today.ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
        MonthMinutes = summary.Rtm.ManagementMinutes;
        MonthHasCall = summary.Rtm.HasInteractiveCommunication;
        MaxDate = today.ToUsDateString();
        MinDate = today.AddDays(-60).ToUsDateString();
        if (string.IsNullOrEmpty(ServiceDate))
        {
            ServiceDate = today.ToUsDateString();
        }
    }

    public async Task<LogTimeSaveData> PrepareDataForSave(ProviderRepositories repos, ICurrentUserService currentUser, DateTimeOffset now, ModelStateDictionary modelState, CancellationToken ct)
    {
        var provider = await CaseloadDataLoader.GetCurrentProviderAsync(repos, currentUser, ct);
        var patient = await repos.Patients.GetForProviderAsync(PatientId, provider.Id, ct)
            ?? throw new EntityNotFoundException($"Patient {PatientId} was not found.");
        var today = now.ToEasternDate();

        if (!FormDates.TryParse(ServiceDate, out var serviceDate))
        {
            modelState.AddModelError(nameof(ServiceDate), "Enter the date as M/d/yyyy.");
        }
        else if (serviceDate > today)
        {
            modelState.AddModelError(nameof(ServiceDate), "The service date can't be in the future.");
        }
        else if (serviceDate < today.AddDays(-60))
        {
            modelState.AddModelError(nameof(ServiceDate), "Time older than 60 days must be corrected by your billing team.");
        }

        if (IsInteractiveCommunication && CommunicationMethod == CommunicationMethod.None)
        {
            modelState.AddModelError(nameof(CommunicationMethod), "Choose how the communication happened.");
        }

        if (!modelState.IsValid)
        {
            return null;
        }

        return new LogTimeSaveData
        {
            PatientName = patient.FullName,
            Entry = new ProviderTimeEntry
            {
                PatientId = patient.Id,
                ProviderId = provider.Id,
                ServiceDate = serviceDate,
                Minutes = Minutes ?? 0,
                Activity = Activity ?? TimeActivity.DataReview,
                IsInteractiveCommunication = IsInteractiveCommunication,
                CommunicationMethod = IsInteractiveCommunication ? CommunicationMethod : CommunicationMethod.None,
                Notes = (Notes ?? string.Empty).Trim(),
                DateCreated = now
            }
        };
    }
}

public sealed class LogTimeSaveData
{
    public ProviderTimeEntry Entry { get; init; }
    public string PatientName { get; init; } = string.Empty;
}
