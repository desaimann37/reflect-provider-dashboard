using Reflect.Domain.Analytics;
using Reflect.Domain.Clinical;
using Reflect.Domain.Common;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.PatientDetails;

public class SessionRowViewModel
{
    public string StartedAt { get; set; } = string.Empty;
    public long StartedAtSort { get; set; }
    public BadgeViewModel Platform { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public double DurationMinutes { get; set; }
    public string MoodChange { get; set; } = string.Empty;
    public bool Completed { get; set; }
    public int CreationsSaved { get; set; }

    public static SessionRowViewModel FromEntity(EngagementSession session, IReadOnlyList<Observation> observations)
    {
        var pre = observations.FirstOrDefault(o => o.SourceSessionId == session.SourceSessionId && o.Code == ObservationCodes.MoodPre);
        var post = observations.FirstOrDefault(o => o.SourceSessionId == session.SourceSessionId && o.Code == ObservationCodes.MoodPost);
        return new SessionRowViewModel
        {
            StartedAt = session.StartedAt.ToUsDateTimeString(),
            StartedAtSort = session.StartedAt.ToUnixTimeSeconds(),
            Platform = ProviderUi.Platform(session.Platform),
            ModuleName = session.ModuleName,
            Duration = ProviderUi.Minutes(session.DurationMinutes),
            DurationMinutes = Math.Round(session.DurationMinutes, 1),
            MoodChange = pre == null ? "No check-in" : post == null ? $"{pre.Value:0} → ?" : $"{pre.Value:0} → {post.Value:0}",
            Completed = session.Completed,
            CreationsSaved = session.CreationsSaved
        };
    }
}
