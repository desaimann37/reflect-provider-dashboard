using Reflect.Domain.Clinical;

namespace Reflect.Domain.Interfaces;

public interface IPatientAppLinkRepository
{
    Task<IReadOnlyList<PatientAppLink>> GetByPatientIdsAsync(IReadOnlyCollection<Guid> patientIds, CancellationToken ct);
}
