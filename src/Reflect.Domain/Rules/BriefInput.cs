namespace Reflect.Domain.Rules;

/// <summary>
/// De-identified facts for a pre-session brief. It holds no name, MRN, contact detail, exact date,
/// or free text the patient wrote. Dates are expressed as "days ago" and age as a 10-year band, so
/// it can be sent to an external model under the HIPAA Safe Harbor approach.
/// </summary>
public sealed record BriefInput
{
    public string AgeBand { get; init; } = string.Empty;
    public string ProgramName { get; init; } = string.Empty;
    public string DiagnosisDescription { get; init; } = string.Empty;
    public int DaysSinceMonitoringStart { get; init; }
    public int TargetSessionsPerWeek { get; init; }
    public int Sessions7Days { get; init; }
    public int Sessions30Days { get; init; }
    public double Minutes30Days { get; init; }
    public int VrSessions30Days { get; init; }
    public int MobileSessions30Days { get; init; }
    public int? DaysSinceLastSession { get; init; }
    public IReadOnlyList<string> TopModules { get; init; } = Array.Empty<string>();
    public decimal MoodLift30Days { get; init; }
    public int MoodCheckIns30Days { get; init; }
    public IReadOnlyList<MeasurePoint> Phq9 { get; init; } = Array.Empty<MeasurePoint>();
    public IReadOnlyList<MeasurePoint> Gad7 { get; init; } = Array.Empty<MeasurePoint>();
    public IReadOnlyList<string> Flags { get; init; } = Array.Empty<string>();
    public string RtmSummary { get; init; } = string.Empty;
}

public sealed record MeasurePoint(int DaysAgo, decimal Score, string Severity);
