using Reflect.Domain.Interfaces;
using Reflect.Domain.Rules;

namespace Reflect.Web.Standins.Data;

/// <summary>
/// STAND-IN for the backend job that refreshes the session read model from BigQuery. Production
/// runs this on a schedule (for example every 15 minutes); the demo runs it once at startup.
/// </summary>
public static class EngagementSync
{
    public static async Task RunAsync(IAnalyticsEventSource source, InMemoryStore store, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct)
    {
        var events = await source.GetEventsAsync(fromUtc, toUtc, ct);
        var sessions = EngagementEventMapper.Map(events);
        lock (store.Sync)
        {
            store.Sessions.Clear();
            store.Sessions.AddRange(sessions);
        }
    }
}
