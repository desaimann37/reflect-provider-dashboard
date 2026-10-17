using Reflect.Domain.Clinical;

namespace Reflect.Domain.Rules;

public sealed record RtmAssessmentInput
{
    public RtmPeriod Period { get; init; }
    public DateOnly Today { get; init; }
    public int DataDays { get; init; }
    public bool TodayIsDataDay { get; init; }

    /// <summary>Treatment-management minutes logged in the calendar month containing <see cref="Today"/>.</summary>
    public int ManagementMinutes { get; init; }

    /// <summary>Real-time interactive communications logged in that calendar month.</summary>
    public int InteractiveCommunications { get; init; }

    public DateOnly? SetupCompletedOn { get; init; }
    public DocumentationStatus DocumentationStatus { get; init; }
    public bool IsActive { get; init; } = true;
}
