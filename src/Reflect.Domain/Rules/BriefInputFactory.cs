using Reflect.Domain.Clinical;
using Reflect.Domain.Common;

namespace Reflect.Domain.Rules;

/// <summary>
/// Builds the de-identified <see cref="BriefInput"/> from a monitoring summary. This is the privacy
/// boundary for anything sent to an external model: only aggregates, relative days, an age band,
/// and the diagnosis description pass through. No name, MRN, date of birth, exact date, or app key.
/// </summary>
public static class BriefInputFactory
{
    public static BriefInput FromSummary(PatientMonitoringSummary summary)
    {
        var today = summary.Today;
        var age = summary.Patient.AgeOn(today);
        var decade = Math.Min(age / 10 * 10, 80);

        return new BriefInput
        {
            AgeBand = age >= 90 ? "90 or older" : $"{decade}-{decade + 9}",
            ProgramName = summary.CarePlan.ProgramName,
            DiagnosisDescription = summary.CarePlan.PrimaryDiagnosisDescription,
            DaysSinceMonitoringStart = today.DayNumber - summary.CarePlan.MonitoringStartDate.DayNumber,
            TargetSessionsPerWeek = summary.CarePlan.TargetSessionsPerWeek,
            Sessions7Days = summary.Sessions7Days,
            Sessions30Days = summary.Sessions30Days,
            Minutes30Days = summary.Minutes30Days,
            VrSessions30Days = summary.VrSessions30Days,
            MobileSessions30Days = summary.MobileSessions30Days,
            DaysSinceLastSession = summary.LastActivityAt.HasValue ? today.DayNumber - summary.LastActivityAt.Value.ToEasternDate().DayNumber : null,
            TopModules = EngagementStats.ModuleMix(EngagementStats.InLastDays(summary.Sessions, today, 30))
                .Take(3)
                .Select(m => $"{m.ModuleName} ({(m.Platform == ReflectPlatform.VirtualReality ? "VR" : "mobile")})")
                .ToList(),
            MoodLift30Days = summary.MoodLift30Days,
            MoodCheckIns30Days = summary.MoodLiftPairs30Days,
            Phq9 = Points(summary.Observations, ObservationCodes.Phq9Total, today, MeasureSeverity.Phq9),
            Gad7 = Points(summary.Observations, ObservationCodes.Gad7Total, today, MeasureSeverity.Gad7),
            Flags = summary.Insights.Where(i => i.NeedsAttention).Select(i => i.Title).ToList(),
            RtmSummary = summary.Rtm == null || summary.Rtm.Readiness == RtmReadiness.Inactive
                ? string.Empty
                : $"RTM period: {summary.Rtm.DataDays} of 16 data days with {summary.Rtm.DaysLeft} days left; {summary.Rtm.ManagementMinutes} management minutes this month{(summary.Rtm.HasInteractiveCommunication ? " including a live call" : ", no live call yet")}."
        };
    }

    private static IReadOnlyList<MeasurePoint> Points(IEnumerable<Observation> observations, string code, DateOnly today, Func<decimal, SeverityBand> band) =>
        observations
            .Where(o => o.Code == code)
            .OrderBy(o => o.EffectiveAt)
            .TakeLast(4)
            .Select(o => new MeasurePoint(today.DayNumber - o.EffectiveAt.ToEasternDate().DayNumber, o.Value, MeasureSeverity.Label(band(o.Value))))
            .ToList();
}
