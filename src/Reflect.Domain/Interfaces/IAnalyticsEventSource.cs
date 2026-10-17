using Reflect.Domain.Analytics;

namespace Reflect.Domain.Interfaces;

/// <summary>
/// The port to raw product analytics. Production: a BigQuery query over the Firebase export,
/// run by the secure backend. Demo: synthetic events in the same shape.
/// </summary>
public interface IAnalyticsEventSource
{
    Task<IReadOnlyList<AnalyticsEvent>> GetEventsAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct);
}
