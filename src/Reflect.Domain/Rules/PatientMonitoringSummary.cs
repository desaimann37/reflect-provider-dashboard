using Reflect.Domain.Analytics;
using Reflect.Domain.Clinical;
using Reflect.Domain.Common;

namespace Reflect.Domain.Rules;

/// <summary>Everything the dashboard needs to know about one patient right now, computed in one pass.</summary>
public sealed class PatientMonitoringSummary
{
    public Patient Patient { get; init; }
    public CarePlan CarePlan { get; init; }
    public PatientAppLink AppLink { get; init; }
    public DateOnly Today { get; init; }

    public IReadOnlyList<EngagementSession> Sessions { get; init; } = Array.Empty<EngagementSession>();
    public IReadOnlyList<Observation> Observations { get; init; } = Array.Empty<Observation>();
    public IReadOnlyList<ProviderTimeEntry> TimeEntries { get; init; } = Array.Empty<ProviderTimeEntry>();
    public IReadOnlyList<RtmPeriodReview> Reviews { get; init; } = Array.Empty<RtmPeriodReview>();

    public DateTimeOffset? LastActivityAt { get; init; }
    public int Sessions7Days { get; init; }
    public int Sessions30Days { get; init; }
    public double Minutes30Days { get; init; }
    public int VrSessions30Days { get; init; }
    public int MobileSessions30Days { get; init; }
    public decimal MoodLift30Days { get; init; }
    public int MoodLiftPairs30Days { get; init; }

    public Observation LatestPhq9 { get; init; }
    public Observation PreviousPhq9 { get; init; }
    public Observation LatestGad7 { get; init; }

    public RtmAssessment Rtm { get; init; }
    public RtmPeriodMetrics PeriodMetrics { get; init; }
    public IReadOnlyList<ClinicalInsight> Insights { get; init; } = Array.Empty<ClinicalInsight>();

    public bool NeedsAttention => Insights.Any(i => i.NeedsAttention);
    public InsightSeverity? TopSeverity => Insights.Count == 0 ? null : Insights.Min(i => i.Severity);
}

/// <summary>The challenge brief's "RTM monitoring-status" metrics for the current period.</summary>
public sealed record RtmPeriodMetrics
{
    public int QualifyingDataDays { get; init; }
    public int Sessions { get; init; }
    public double EngagementMinutes { get; init; }
    public int TherapeuticResponseEntries { get; init; }
    public int ProviderReviews { get; init; }
    public int PatientCommunications { get; init; }
    public int ManagementMinutes { get; init; }
    public DocumentationStatus DocumentationStatus { get; init; }
}

public static class PatientMonitoringSummaryBuilder
{
    /// <summary>
    /// A day qualifies when the patient completed a session or reported therapeutic-response data.
    /// </summary>
    public static IEnumerable<DateOnly> QualifyingDays(IEnumerable<EngagementSession> sessions, IEnumerable<Observation> observations) =>
        sessions.Where(s => s.Completed).Select(s => s.StartedAt.ToEasternDate())
            .Concat(observations.Where(o => o.Source == ObservationSource.PatientReported).Select(o => o.EffectiveAt.ToEasternDate()))
            .Distinct();

    public static PatientMonitoringSummary Build(
        Patient patient,
        CarePlan carePlan,
        PatientAppLink appLink,
        IReadOnlyList<EngagementSession> sessions,
        IReadOnlyList<Observation> observations,
        IReadOnlyList<ProviderTimeEntry> timeEntries,
        IReadOnlyList<RtmPeriodReview> reviews,
        DateTimeOffset now,
        RtmRules rules)
    {
        var today = now.ToEasternDate();
        sessions ??= Array.Empty<EngagementSession>();
        observations ??= Array.Empty<Observation>();
        timeEntries ??= Array.Empty<ProviderTimeEntry>();
        reviews ??= Array.Empty<RtmPeriodReview>();

        var last30 = EngagementStats.InLastDays(sessions, today, 30).ToList();
        var period = RtmCalculator.PeriodContaining(carePlan.MonitoringStartDate, today, rules);
        var month = RtmCalculator.CalendarMonth(today);

        var qualifying = QualifyingDays(sessions, observations).ToList();
        var dataDays = RtmCalculator.CountDataDays(qualifying, period);

        var monthEntries = timeEntries.Where(t => t.ServiceDate >= month.Start && t.ServiceDate <= month.End).ToList();
        var managementMinutes = monthEntries.Sum(t => t.Minutes);
        var communications = monthEntries.Count(t => t.IsInteractiveCommunication);

        var review = reviews.FirstOrDefault(r => r.PeriodStart == period.Start);
        var documentation = review?.Status
            ?? (monthEntries.Count > 0 ? DocumentationStatus.InProgress : DocumentationStatus.NotStarted);

        var isActive = carePlan.Status == CarePlanStatus.Active && patient.Status == PatientStatus.Active;
        var rtm = RtmCalculator.Assess(new RtmAssessmentInput
        {
            Period = period,
            Today = today,
            DataDays = dataDays,
            TodayIsDataDay = qualifying.Contains(today),
            ManagementMinutes = managementMinutes,
            InteractiveCommunications = communications,
            SetupCompletedOn = carePlan.SetupCompletedOn,
            DocumentationStatus = documentation,
            IsActive = isActive
        }, rules);

        var periodSessions = sessions.Where(s => period.Contains(s.StartedAt.ToEasternDate())).ToList();
        var periodObservations = observations.Where(o => period.Contains(o.EffectiveAt.ToEasternDate())).ToList();

        var metrics = new RtmPeriodMetrics
        {
            QualifyingDataDays = dataDays,
            Sessions = periodSessions.Count,
            EngagementMinutes = Math.Round(periodSessions.Sum(s => s.DurationMinutes)),
            TherapeuticResponseEntries = periodObservations.Count(o => o.Code is ObservationCodes.MoodPre or ObservationCodes.Phq9Total or ObservationCodes.Gad7Total),
            ProviderReviews = timeEntries.Count(t => t.Activity == TimeActivity.DataReview && period.Contains(t.ServiceDate)),
            PatientCommunications = communications,
            ManagementMinutes = managementMinutes,
            DocumentationStatus = documentation
        };

        var insights = InsightEngine.Evaluate(new InsightInput
        {
            Today = today,
            MonitoringStartDate = carePlan.MonitoringStartDate,
            Sessions = sessions,
            Observations = observations,
            Rtm = rtm,
            IsMonitoringActive = isActive
        });

        var lift = InsightEngine.AverageMoodLift(observations, today.AddDays(-29), today, out var pairs);
        var phq = observations.Where(o => o.Code == ObservationCodes.Phq9Total).OrderByDescending(o => o.EffectiveAt).Take(2).ToList();

        return new PatientMonitoringSummary
        {
            Patient = patient,
            CarePlan = carePlan,
            AppLink = appLink,
            Today = today,
            Sessions = sessions,
            Observations = observations,
            TimeEntries = timeEntries,
            Reviews = reviews,
            LastActivityAt = sessions.Count == 0 ? null : sessions.Max(s => s.StartedAt),
            Sessions7Days = EngagementStats.InLastDays(sessions, today, 7).Count(),
            Sessions30Days = last30.Count,
            Minutes30Days = Math.Round(last30.Sum(s => s.DurationMinutes)),
            VrSessions30Days = last30.Count(s => s.Platform == ReflectPlatform.VirtualReality),
            MobileSessions30Days = last30.Count(s => s.Platform == ReflectPlatform.Mobile),
            MoodLift30Days = lift,
            MoodLiftPairs30Days = pairs,
            LatestPhq9 = phq.FirstOrDefault(),
            PreviousPhq9 = phq.Skip(1).FirstOrDefault(),
            LatestGad7 = observations.Where(o => o.Code == ObservationCodes.Gad7Total).OrderByDescending(o => o.EffectiveAt).FirstOrDefault(),
            Rtm = rtm,
            PeriodMetrics = metrics,
            Insights = insights
        };
    }
}
