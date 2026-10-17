using Reflect.Domain.Clinical;

namespace Reflect.Domain.Interfaces;

public interface IProviderProfileRepository
{
    Task<ProviderProfile> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<ProviderProfile> GetByIdAsync(Guid id, CancellationToken ct);
}
