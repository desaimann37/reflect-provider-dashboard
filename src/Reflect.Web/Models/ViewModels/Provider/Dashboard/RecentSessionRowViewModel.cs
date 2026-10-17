using Reflect.Domain.Analytics;
using Reflect.Domain.Clinical;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.Dashboard;

public class RecentSessionRowViewModel
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public string When { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public BadgeViewModel Platform { get; set; }

    public static RecentSessionRowViewModel FromEntity(Patient patient, EngagementSession session, DateTimeOffset now) => new()
    {
        PatientId = patient.Id,
        PatientName = patient.FullName,
        Initials = patient.Initials,
        ModuleName = session.ModuleName,
        When = ProviderUi.Relative(session.StartedAt, now),
        Duration = ProviderUi.Minutes(session.DurationMinutes),
        Platform = ProviderUi.Platform(session.Platform)
    };
}
