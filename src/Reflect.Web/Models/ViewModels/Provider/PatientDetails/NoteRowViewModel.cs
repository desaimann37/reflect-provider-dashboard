using Reflect.Domain.Clinical;
using Reflect.Domain.Common;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.PatientDetails;

public class NoteRowViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string AuthoredAt { get; set; } = string.Empty;

    public static NoteRowViewModel FromEntity(ClinicalNote note) => new()
    {
        Title = note.Title,
        Type = ProviderUi.NoteTypeLabel(note.NoteType),
        Body = note.Body,
        Author = note.AuthorName,
        AuthoredAt = note.AuthoredAt.ToUsDateTimeString()
    };
}
