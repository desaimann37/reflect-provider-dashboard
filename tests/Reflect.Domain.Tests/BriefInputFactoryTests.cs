using Reflect.Domain.Clinical;
using Reflect.Domain.Rules;
using static Reflect.Domain.Tests.TestData;

namespace Reflect.Domain.Tests;

public class BriefInputFactoryTests
{
    [Fact]
    public void Brief_input_contains_no_direct_identifiers()
    {
        var today = new DateOnly(2026, 10, 16);
        var patient = new Patient
        {
            FirstName = "Zebulon",
            LastName = "Quixote",
            Mrn = "RB-999111",
            DateOfBirth = new DateOnly(1987, 5, 3)
        };
        var plan = new CarePlan
        {
            ProgramName = "Anxiety care",
            PrimaryDiagnosisDescription = "Generalized anxiety disorder",
            MonitoringStartDate = today.AddDays(-40),
            TargetSessionsPerWeek = 3,
            Status = CarePlanStatus.Active
        };
        var link = new PatientAppLink { AppUserKey = "rx_secretkey123" };
        var sessions = new[] { Session(today.AddDays(-1), id: "sess-1"), Session(today.AddDays(-3), id: "sess-2") };
        var observations = new[]
        {
            Obs(ObservationCodes.Phq9Total, 14, today.AddDays(-30)),
            Obs(ObservationCodes.Phq9Total, 9, today.AddDays(-2)),
            Obs(ObservationCodes.MoodPre, 4, today.AddDays(-1), "sess-1"),
            Obs(ObservationCodes.MoodPost, 6, today.AddDays(-1), "sess-1")
        };

        var summary = PatientMonitoringSummaryBuilder.Build(patient, plan, link, sessions, observations, null, null, NoonEt(today), RtmRules.Default);
        var input = BriefInputFactory.FromSummary(summary);
        var json = System.Text.Json.JsonSerializer.Serialize(input);

        Assert.DoesNotContain("Zebulon", json);
        Assert.DoesNotContain("Quixote", json);
        Assert.DoesNotContain("RB-999111", json);
        Assert.DoesNotContain("rx_secretkey123", json);
        Assert.DoesNotContain("1987", json);
        Assert.DoesNotContain("2026", json);
        Assert.Equal("30-39", input.AgeBand);
        Assert.Equal(new[] { 30, 2 }, input.Phq9.Select(p => p.DaysAgo));
    }
}
