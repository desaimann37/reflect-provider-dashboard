using System.Globalization;
using Reflect.Domain.Analytics;
using Reflect.Domain.Common;

namespace Reflect.Domain.Rules;

/// <summary>
/// Turns raw analytics events (Firebase / GA4 BigQuery export shape) into sessions. In production
/// this runs in the backend sync job after a BigQuery query; the dashboard only reads its output.
/// </summary>
public static class EngagementEventMapper
{
    public static IReadOnlyList<EngagementSession> Map(IEnumerable<AnalyticsEvent> events)
    {
        var sessions = new List<EngagementSession>();
        var groups = events
            .Where(e => !string.IsNullOrEmpty(e.UserPseudoId) && !string.IsNullOrEmpty(e.Param(AnalyticsEventNames.ParamSessionId)))
            .GroupBy(e => (e.UserPseudoId, SessionId: e.Param(AnalyticsEventNames.ParamSessionId)));

        foreach (var group in groups)
        {
            var ordered = group.OrderBy(e => e.EventTimestampMicros).ToList();
            var start = ordered.FirstOrDefault(e => e.EventName == AnalyticsEventNames.ModuleStart) ?? ordered[0];
            var complete = ordered.LastOrDefault(e => e.EventName == AnalyticsEventNames.ModuleComplete);
            var last = ordered[^1];

            var durationSeconds = complete != null
                && int.TryParse(complete.Param(AnalyticsEventNames.ParamDurationSeconds), NumberStyles.Integer, CultureInfo.InvariantCulture, out var reported)
                ? reported
                : (int)Math.Round((last.Timestamp - start.Timestamp).TotalSeconds);

            sessions.Add(new EngagementSession
            {
                AppUserKey = group.Key.UserPseudoId,
                SourceSessionId = group.Key.SessionId,
                Platform = start.AppId == AnalyticsEventNames.ReflectXrAppId ? ReflectPlatform.VirtualReality : ReflectPlatform.Mobile,
                ModuleKey = start.Param(AnalyticsEventNames.ParamModuleId),
                ModuleName = start.Param(AnalyticsEventNames.ParamModuleName),
                StartedAt = start.Timestamp,
                DurationSeconds = Math.Max(0, durationSeconds),
                Completed = complete != null,
                CreationsSaved = ordered.Count(e => e.EventName == AnalyticsEventNames.CreationSaved),
                DateCreated = start.Timestamp
            });
        }

        return sessions.OrderBy(s => s.StartedAt).ToList();
    }
}
