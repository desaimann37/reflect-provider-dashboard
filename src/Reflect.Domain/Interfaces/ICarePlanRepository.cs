using Reflect.Domain.Clinical;

namespace Reflect.Domain.Interfaces;

public interface ICarePlanRepository
{
    Task<CarePlan> GetByPatientIdAsync(Guid patientId, CancellationToken ct);
    Task<IReadOnlyList<CarePlan>> GetByPatientIdsAsync(IReadOnlyCollection<Guid> patientIds, CancellationToken ct);
    Task UpdateAsync(CarePlan carePlan, CancellationToken ct);
}
