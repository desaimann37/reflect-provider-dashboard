namespace Reflect.Domain.Rules;

public enum InsightKind
{
    SafetyScreenPositive = 1,
    Phq9Worsening = 2,
    Gad7Worsening = 3,
    Phq9Improving = 4,
    Inactivity = 5,
    EngagementDrop = 6,
    MoodDecline = 7,
    MoodLift = 8,
    NoDataSinceEnrollment = 9,
    RtmAtRisk = 10,
    RtmReadyToSign = 11
}

public enum InsightSeverity
{
    Critical = 0,
    High = 1,
    Medium = 2,
    Low = 3,
    Positive = 4
}

public sealed record ClinicalInsight(InsightKind Kind, InsightSeverity Severity, string Title, string Detail, DateTimeOffset? ObservedAt = null)
{
    public bool NeedsAttention => Severity <= InsightSeverity.Medium;
}
