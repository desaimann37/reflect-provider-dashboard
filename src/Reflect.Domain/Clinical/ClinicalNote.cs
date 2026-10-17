using Reflect.Domain.Common;

namespace Reflect.Domain.Clinical;

public class ClinicalNote : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid ProviderId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public NoteType NoteType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset AuthoredAt { get; set; }
}
