using Reflect.Domain.Clinical;
using Reflect.Domain.Rules;
using static Reflect.Domain.Tests.TestData;

namespace Reflect.Domain.Tests;

public class MonitoringSummaryTests
{
    private static readonly DateOnly Today = new(2026, 10, 16);

    private static (Patient, CarePlan) Patient(DateOnly start) =>
        (new Patient { FirstName = "Test", LastName = "Patient", DateOfBirth = new DateOnly(1990, 1, 1) },
         new CarePlan { MonitoringStartDate = start, Status = CarePlanStatus.Active, TargetSessionsPerWeek = 3 });

    [Fact]
    public void Observation_only_days_count_as_data_days_and_incomplete_sessions_do_not()
    {
        var (patient, plan) = Patient(new DateOnly(2026, 10, 1));
        var sessions = new[]
        {
            Session(new DateOnly(2026, 10, 2)),
            Session(new DateOnly(2026, 10, 3), completed: false),
            Session(new DateOnly(2026, 10, 4)),
            Session(new DateOnly(2026, 10, 4))
        };
        var obs = new[] { Obs(ObservationCodes.Phq9Total, 8, new DateOnly(2026, 10, 9)) };

        var summary = PatientMonitoringSummaryBuilder.Build(patient, plan, null, sessions, obs, null, null, NoonEt(Today), RtmRules.Default);

        Assert.Equal(3, summary.PeriodMetrics.QualifyingDataDays);
        Assert.Equal(4, summary.PeriodMetrics.Sessions);
        Assert.Equal(1, summary.PeriodMetrics.TherapeuticResponseEntries);
        Assert.Equal(DocumentationStatus.NotStarted, summary.PeriodMetrics.DocumentationStatus);
    }

    [Fact]
    public void Time_logged_this_month_marks_documentation_in_progress_and_counts_calls()
    {
        var (patient, plan) = Patient(new DateOnly(2026, 9, 20));
        var entries = new[]
        {
            new ProviderTimeEntry { ServiceDate = new DateOnly(2026, 9, 28), Minutes = 30, IsInteractiveCommunication = true },
            new ProviderTimeEntry { ServiceDate = new DateOnly(2026, 10, 5), Minutes = 8, Activity = TimeActivity.DataReview },
            new ProviderTimeEntry { ServiceDate = new DateOnly(2026, 10, 12), Minutes = 15, IsInteractiveCommunication = true, Activity = TimeActivity.InteractiveCommunication }
        };

        var summary = PatientMonitoringSummaryBuilder.Build(patient, plan, null, null, null, entries, null, NoonEt(Today), RtmRules.Default);

        Assert.Equal(23, summary.PeriodMetrics.ManagementMinutes);
        Assert.Equal(1, summary.PeriodMetrics.PatientCommunications);
        Assert.Equal(1, summary.PeriodMetrics.ProviderReviews);
        Assert.Equal(DocumentationStatus.InProgress, summary.PeriodMetrics.DocumentationStatus);
        Assert.Equal(new[] { "98980" }, summary.Rtm.ManagementCodes.Select(c => c.Code));
    }

    [Fact]
    public void Signed_review_for_current_period_is_reflected()
    {
        var (patient, plan) = Patient(new DateOnly(2026, 10, 1));
        var review = new RtmPeriodReview { PeriodStart = new DateOnly(2026, 10, 1), Status = DocumentationStatus.Signed };

        var summary = PatientMonitoringSummaryBuilder.Build(patient, plan, null, null, null, null, new[] { review }, NoonEt(Today), RtmRules.Default);

        Assert.Equal(RtmReadiness.Signed, summary.Rtm.Readiness);
    }
}
