using Reflect.Domain.Clinical;

namespace Reflect.Domain.Interfaces;

public interface IRtmPeriodReviewRepository
{
    Task<IReadOnlyList<RtmPeriodReview>> GetByPatientIdsAsync(IReadOnlyCollection<Guid> patientIds, CancellationToken ct);
    Task<RtmPeriodReview> GetAsync(Guid patientId, DateOnly periodStart, CancellationToken ct);
    Task AddAsync(RtmPeriodReview review, CancellationToken ct);
    Task UpdateAsync(RtmPeriodReview review, CancellationToken ct);
}
