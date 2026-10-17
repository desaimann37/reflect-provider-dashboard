using Reflect.Domain.Clinical;

namespace Reflect.Domain.Interfaces;

public interface IProviderTimeEntryRepository
{
    Task<IReadOnlyList<ProviderTimeEntry>> GetByPatientIdsAsync(
        IReadOnlyCollection<Guid> patientIds, DateOnly from, DateOnly to, CancellationToken ct);

    Task AddAsync(ProviderTimeEntry entry, CancellationToken ct);
}
