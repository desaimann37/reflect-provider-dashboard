using Reflect.Domain.Clinical;

namespace Reflect.Domain.Interfaces;

public interface IClinicalNoteRepository
{
    Task<IReadOnlyList<ClinicalNote>> GetByPatientIdAsync(Guid patientId, CancellationToken ct);
    Task<IReadOnlyList<ClinicalNote>> GetByPatientIdsAsync(IReadOnlyCollection<Guid> patientIds, DateTimeOffset fromUtc, CancellationToken ct);
    Task AddAsync(ClinicalNote note, CancellationToken ct);
}
