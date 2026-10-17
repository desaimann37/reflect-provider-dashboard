using Reflect.Domain.Clinical;

namespace Reflect.Domain.Interfaces;

public interface IObservationRepository
{
    Task<IReadOnlyList<Observation>> GetByPatientIdsAsync(
        IReadOnlyCollection<Guid> patientIds, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct);

    Task AddAsync(Observation observation, CancellationToken ct);
}
