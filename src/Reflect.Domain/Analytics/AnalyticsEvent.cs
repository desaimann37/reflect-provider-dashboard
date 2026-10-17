namespace Reflect.Domain.Analytics;

/// <summary>
/// One row in the shape of the Firebase / GA4 BigQuery export (<c>events_YYYYMMDD</c>), trimmed to
/// the fields the dashboard needs. <see cref="Params"/> flattens <c>event_params</c> (key to string
/// value). <see cref="AppId"/> is <c>app_info.id</c>, which tells Reflect XR and Reflect Mobile apart.
/// </summary>
public sealed record AnalyticsEvent(
    string UserPseudoId,
    string EventName,
    long EventTimestampMicros,
    string AppId,
    IReadOnlyDictionary<string, string> Params)
{
    public DateTimeOffset Timestamp => DateTimeOffset.FromUnixTimeMilliseconds(EventTimestampMicros / 1000);

    public string Param(string key) =>
        Params != null && Params.TryGetValue(key, out var value) ? value : string.Empty;
}
