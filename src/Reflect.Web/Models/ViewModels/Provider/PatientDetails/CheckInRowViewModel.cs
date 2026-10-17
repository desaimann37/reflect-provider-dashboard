using Reflect.Domain.Analytics;
using Reflect.Domain.Clinical;
using Reflect.Domain.Common;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.PatientDetails;

public class CheckInRowViewModel
{
    public string When { get; set; } = string.Empty;
    public long SortKey { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public BadgeViewModel Platform { get; set; }
    public string Before { get; set; } = string.Empty;
    public string After { get; set; } = string.Empty;
    public string Change { get; set; } = string.Empty;
    public string ChangeDirection { get; set; } = string.Empty;

    public static IEnumerable<CheckInRowViewModel> FromObservations(IReadOnlyList<Observation> observations, IReadOnlyList<EngagementSession> sessions)
    {
        var sessionById = sessions.Where(s => !string.IsNullOrEmpty(s.SourceSessionId))
            .GroupBy(s => s.SourceSessionId)
            .ToDictionary(g => g.Key, g => g.First());

        return observations
            .Where(o => o.Code == ObservationCodes.MoodPre)
            .OrderByDescending(o => o.EffectiveAt)
            .Select(pre =>
            {
                var post = observations.FirstOrDefault(o => o.Code == ObservationCodes.MoodPost && o.SourceSessionId == pre.SourceSessionId);
                sessionById.TryGetValue(pre.SourceSessionId, out var session);
                var change = post == null ? (decimal?)null : post.Value - pre.Value;
                return new CheckInRowViewModel
                {
                    When = pre.EffectiveAt.ToUsDateTimeString(),
                    SortKey = pre.EffectiveAt.ToUnixTimeSeconds(),
                    ModuleName = session?.ModuleName ?? "Check-in",
                    Platform = ProviderUi.Platform(pre.Platform ?? ReflectPlatform.Mobile),
                    Before = $"{pre.Value:0}",
                    After = post == null ? "Not recorded" : $"{post.Value:0}",
                    Change = change switch { null => "n/a", > 0 => $"+{change:0}", _ => $"{change:0}" },
                    ChangeDirection = change switch { null or 0 => "flat", > 0 => "up", _ => "down" }
                };
            });
    }
}
