using Reflect.Domain.Analytics;

namespace Reflect.Domain.Interfaces;

/// <summary>
/// Read model of app sessions built from analytics events. In production this is a curated table
/// refreshed from the Firebase BigQuery export; in the demo it is in memory.
/// </summary>
public interface IEngagementSessionRepository
{
    Task<IReadOnlyList<EngagementSession>> GetByAppUserKeysAsync(
        IReadOnlyCollection<string> appUserKeys, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct);
}
