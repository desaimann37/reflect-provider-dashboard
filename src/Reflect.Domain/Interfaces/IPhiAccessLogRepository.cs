using Reflect.Domain.Clinical;

namespace Reflect.Domain.Interfaces;

public interface IPhiAccessLogRepository
{
    Task AddAsync(PhiAccessLog entry, CancellationToken ct);
    Task<IReadOnlyList<PhiAccessLog>> GetRecentByPatientIdAsync(Guid patientId, int take, CancellationToken ct);
}
