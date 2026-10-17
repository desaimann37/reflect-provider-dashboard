using Reflect.Domain.Clinical;
using Reflect.Domain.Rules;

namespace Reflect.Domain.Tests;

public class RtmCalculatorTests
{
    private static readonly RtmRules Rules = RtmRules.Default;
    private static readonly DateOnly Start = new(2026, 9, 1);

    private static RtmAssessment Assess(int dataDays, int minutes, int comms, DateOnly today,
        DocumentationStatus status = DocumentationStatus.InProgress, bool todayIsDataDay = false, DateOnly? setup = null, bool active = true)
    {
        var period = RtmCalculator.PeriodContaining(Start, today, Rules);
        return RtmCalculator.Assess(new RtmAssessmentInput
        {
            Period = period,
            Today = today,
            DataDays = dataDays,
            TodayIsDataDay = todayIsDataDay,
            ManagementMinutes = minutes,
            InteractiveCommunications = comms,
            SetupCompletedOn = setup,
            DocumentationStatus = status,
            IsActive = active
        }, Rules);
    }

    [Fact]
    public void Periods_are_consecutive_30_day_windows_from_monitoring_start()
    {
        var period = RtmCalculator.PeriodContaining(Start, new DateOnly(2026, 10, 16), Rules);

        Assert.Equal(new DateOnly(2026, 10, 1), period.Start);
        Assert.Equal(new DateOnly(2026, 10, 30), period.End);
        Assert.Equal(30, period.LengthDays);
        Assert.Equal(16, period.DayOfPeriod(new DateOnly(2026, 10, 16)));
        Assert.Equal(15, period.DaysLeftIncludingToday(new DateOnly(2026, 10, 16)));
    }

    [Fact]
    public void Day_before_monitoring_start_maps_to_first_period()
    {
        var period = RtmCalculator.PeriodContaining(Start, new DateOnly(2026, 8, 20), Rules);

        Assert.Equal(Start, period.Start);
    }

    [Fact]
    public void PeriodsThrough_lists_every_period_newest_first()
    {
        var periods = RtmCalculator.PeriodsThrough(Start, new DateOnly(2026, 11, 5), Rules);

        Assert.Equal(3, periods.Count);
        Assert.Equal(new DateOnly(2026, 10, 31), periods[0].Start);
        Assert.Equal(Start, periods[^1].Start);
    }

    [Fact]
    public void Data_days_are_distinct_and_limited_to_the_period()
    {
        var period = new RtmPeriod(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 30));
        var days = new[]
        {
            new DateOnly(2026, 9, 30), new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 1),
            new DateOnly(2026, 10, 15), new DateOnly(2026, 10, 30), new DateOnly(2026, 10, 31)
        };

        Assert.Equal(3, RtmCalculator.CountDataDays(days, period));
    }

    [Fact]
    public void Sixteen_days_and_twenty_minutes_with_a_call_is_ready_to_sign()
    {
        var result = Assess(dataDays: 16, minutes: 20, comms: 1, today: new DateOnly(2026, 10, 20));

        Assert.Equal(RtmReadiness.ReadyToSign, result.Readiness);
        Assert.Equal(RtmCodes.DeviceSupplyFull, result.DeviceSupplyCode);
        Assert.Equal(new[] { "98978", "98980" }, result.EligibleCodes.Select(c => c.Label));
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Between_two_and_fifteen_days_uses_the_short_supply_code()
    {
        var result = Assess(dataDays: 5, minutes: 0, comms: 0, today: new DateOnly(2026, 10, 26));

        Assert.Equal(RtmCodes.DeviceSupplyShort, result.DeviceSupplyCode);
        Assert.Equal(RtmReadiness.AtRisk, result.Readiness);
        Assert.Equal(10, result.MaxReachableDataDays);
        Assert.Contains(result.Issues, i => i.StartsWith("Cannot reach 16"));
    }

    [Fact]
    public void One_data_day_supports_no_device_code()
    {
        var result = Assess(dataDays: 1, minutes: 0, comms: 0, today: new DateOnly(2026, 10, 2));

        Assert.Equal(string.Empty, result.DeviceSupplyCode);
        Assert.Equal(RtmReadiness.OnTrack, result.Readiness);
    }

    [Fact]
    public void On_pace_patient_with_a_call_is_on_track()
    {
        var result = Assess(dataDays: 12, minutes: 15, comms: 1, today: new DateOnly(2026, 10, 18));

        Assert.Equal(RtmReadiness.OnTrack, result.Readiness);
        Assert.Equal(4, result.DataDaysNeededForFullSupply);
        Assert.Equal(new[] { "98986", "98979" }, result.EligibleCodes.Select(c => c.Code));
    }

    [Fact]
    public void Needing_data_on_nearly_every_remaining_day_is_at_risk()
    {
        // 8 days so far, 9 open days left: needs 8 of the next 9 days.
        var result = Assess(dataDays: 8, minutes: 30, comms: 1, today: new DateOnly(2026, 10, 22));

        Assert.Equal(RtmReadiness.AtRisk, result.Readiness);
    }

    [Fact]
    public void Last_week_without_interactive_communication_is_at_risk()
    {
        var result = Assess(dataDays: 15, minutes: 25, comms: 0, today: new DateOnly(2026, 10, 25));

        Assert.Equal(RtmReadiness.AtRisk, result.Readiness);
        Assert.Empty(result.ManagementCodes);
        Assert.Contains(result.Issues, i => i.Contains("interactive communication"));
    }

    [Theory]
    [InlineData(9, true, new string[0])]
    [InlineData(10, true, new[] { "98979" })]
    [InlineData(19, true, new[] { "98979" })]
    [InlineData(20, true, new[] { "98980" })]
    [InlineData(39, true, new[] { "98980" })]
    [InlineData(40, true, new[] { "98980", "98981" })]
    [InlineData(65, true, new[] { "98980", "98981 x2" })]
    [InlineData(45, false, new string[0])]
    public void Management_codes_follow_minute_thresholds(int minutes, bool call, string[] expected)
    {
        var codes = RtmCalculator.ManagementCodes(minutes, call, Rules);

        Assert.Equal(expected, codes.Select(c => c.Label));
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(12, 8)]
    [InlineData(20, 20)]
    [InlineData(25, 15)]
    [InlineData(41, 19)]
    public void Minutes_to_next_threshold(int minutes, int expected)
    {
        var result = Assess(dataDays: 10, minutes: minutes, comms: 1, today: new DateOnly(2026, 10, 10));

        Assert.Equal(expected, result.MinutesToNextThreshold);
    }

    [Fact]
    public void Setup_completed_in_the_period_adds_98975()
    {
        var result = Assess(dataDays: 3, minutes: 0, comms: 0, today: new DateOnly(2026, 9, 5), setup: new DateOnly(2026, 9, 1));

        Assert.True(result.SetupCompletedThisPeriod);
        Assert.Equal("98975", result.EligibleCodes[0].Code);
    }

    [Fact]
    public void Signed_and_inactive_states_take_priority()
    {
        Assert.Equal(RtmReadiness.Signed, Assess(2, 0, 0, new DateOnly(2026, 10, 29), DocumentationStatus.Signed).Readiness);
        Assert.Equal(RtmReadiness.Inactive, Assess(20, 30, 1, new DateOnly(2026, 10, 29), active: false).Readiness);
    }

    [Fact]
    public void Last_day_with_partial_supply_and_management_is_ready_to_sign()
    {
        var result = Assess(dataDays: 9, minutes: 22, comms: 1, today: new DateOnly(2026, 10, 30), todayIsDataDay: true);

        Assert.Equal(RtmReadiness.ReadyToSign, result.Readiness);
        Assert.Equal(new[] { "98986", "98980" }, result.EligibleCodes.Select(c => c.Code));
    }
}
