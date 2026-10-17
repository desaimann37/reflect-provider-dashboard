using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Reflect.Domain.Clinical;
using Reflect.Domain.Common;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.AddNote;

public class AddNoteViewModel
{
    [Required]
    public Guid PatientId { get; set; }

    [Required(ErrorMessage = "Choose a note type.")]
    [Display(Name = "Note type")]
    public NoteType? NoteType { get; set; }

    [Required(ErrorMessage = "Enter a title.")]
    [StringLength(120, ErrorMessage = "The title can be up to 120 characters.")]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the note.")]
    [StringLength(4000, ErrorMessage = "The note can be up to 4,000 characters.")]
    [Display(Name = "Note")]
    public string Body { get; set; } = string.Empty;

    public string PatientName { get; set; } = string.Empty;
    public string Mrn { get; set; } = string.Empty;

    public List<SelectListItem> NoteTypeOptions => Enum.GetValues<NoteType>()
        .Select(t => new SelectListItem(ProviderUi.NoteTypeLabel(t), t.ToString(), NoteType == t))
        .ToList();

    public async Task PrepareData(ProviderRepositories repos, ICurrentUserService currentUser, Guid patientId, CancellationToken ct)
    {
        var provider = await CaseloadDataLoader.GetCurrentProviderAsync(repos, currentUser, ct);
        var patient = await repos.Patients.GetForProviderAsync(patientId, provider.Id, ct)
            ?? throw new EntityNotFoundException($"Patient {patientId} was not found.");
        PatientId = patient.Id;
        PatientName = patient.FullName;
        Mrn = patient.Mrn;
    }

    public async Task<AddNoteSaveData> PrepareDataForSave(ProviderRepositories repos, ICurrentUserService currentUser, DateTimeOffset now, ModelStateDictionary modelState, CancellationToken ct)
    {
        var provider = await CaseloadDataLoader.GetCurrentProviderAsync(repos, currentUser, ct);
        var patient = await repos.Patients.GetForProviderAsync(PatientId, provider.Id, ct)
            ?? throw new EntityNotFoundException($"Patient {PatientId} was not found.");

        if (!modelState.IsValid)
        {
            return null;
        }

        return new AddNoteSaveData
        {
            PatientName = patient.FullName,
            Note = new ClinicalNote
            {
                PatientId = patient.Id,
                ProviderId = provider.Id,
                AuthorName = provider.DisplayName,
                NoteType = NoteType ?? Reflect.Domain.Clinical.NoteType.DataReview,
                Title = Title.Trim(),
                Body = Body.Trim(),
                AuthoredAt = now,
                DateCreated = now
            }
        };
    }
}

public sealed class AddNoteSaveData
{
    public ClinicalNote Note { get; init; }
    public string PatientName { get; init; } = string.Empty;
}
