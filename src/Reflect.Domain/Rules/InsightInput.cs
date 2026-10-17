using Reflect.Domain.Analytics;
using Reflect.Domain.Clinical;

namespace Reflect.Domain.Rules;

public sealed record InsightInput
{
    public DateOnly Today { get; init; }
    public DateOnly MonitoringStartDate { get; init; }
    public IReadOnlyList<EngagementSession> Sessions { get; init; } = Array.Empty<EngagementSession>();
    public IReadOnlyList<Observation> Observations { get; init; } = Array.Empty<Observation>();
    public RtmAssessment Rtm { get; init; }

    /// <summary>False when the care plan is paused or completed: engagement and RTM rules are skipped.</summary>
    public bool IsMonitoringActive { get; init; } = true;
}
