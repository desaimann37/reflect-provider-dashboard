using Reflect.Domain.Clinical;

namespace Reflect.Domain.Rules;

/// <summary>
/// Pure RTM logic. Data days are counted per 30-day period that starts on the monitoring start
/// date. Management time is counted per calendar month, as the CPT descriptors require. Results are
/// decision support for documentation, not billing advice.
/// </summary>
public static class RtmCalculator
{
    public static RtmPeriod PeriodContaining(DateOnly monitoringStart, DateOnly day, RtmRules rules)
    {
        if (day < monitoringStart)
        {
            day = monitoringStart;
        }
        var index = (day.DayNumber - monitoringStart.DayNumber) / rules.PeriodLengthDays;
        var start = monitoringStart.AddDays(index * rules.PeriodLengthDays);
        return new RtmPeriod(start, start.AddDays(rules.PeriodLengthDays - 1));
    }

    /// <summary>All periods from the monitoring start through the period containing <paramref name="through"/>, newest first.</summary>
    public static IReadOnlyList<RtmPeriod> PeriodsThrough(DateOnly monitoringStart, DateOnly through, RtmRules rules)
    {
        var periods = new List<RtmPeriod>();
        if (through < monitoringStart)
        {
            return periods;
        }
        var current = PeriodContaining(monitoringStart, through, rules);
        for (var start = monitoringStart; start <= current.Start; start = start.AddDays(rules.PeriodLengthDays))
        {
            periods.Add(new RtmPeriod(start, start.AddDays(rules.PeriodLengthDays - 1)));
        }
        periods.Reverse();
        return periods;
    }

    public static (DateOnly Start, DateOnly End) CalendarMonth(DateOnly day)
    {
        var start = new DateOnly(day.Year, day.Month, 1);
        return (start, start.AddMonths(1).AddDays(-1));
    }

    public static int CountDataDays(IEnumerable<DateOnly> qualifyingDays, RtmPeriod period) =>
        qualifyingDays.Where(period.Contains).Distinct().Count();

    public static RtmAssessment Assess(RtmAssessmentInput input, RtmRules rules)
    {
        var period = input.Period;
        var daysLeft = period.DaysLeftIncludingToday(input.Today);
        var openDays = Math.Max(0, daysLeft - (input.TodayIsDataDay ? 1 : 0));
        var maxReachable = input.DataDays + openDays;
        var needed = Math.Max(0, rules.FullSupplyMinDays - input.DataDays);

        var deviceCode = input.DataDays >= rules.FullSupplyMinDays
            ? RtmCodes.DeviceSupplyFull
            : input.DataDays >= rules.ShortSupplyMinDays ? RtmCodes.DeviceSupplyShort : string.Empty;

        var minutes = Math.Max(0, input.ManagementMinutes);
        var hasComm = input.InteractiveCommunications > 0;
        var managementCodes = ManagementCodes(minutes, hasComm, rules);
        var nextThreshold = minutes < rules.BriefManagementMinutes
            ? rules.BriefManagementMinutes
            : minutes < rules.FullManagementMinutes
                ? rules.FullManagementMinutes
                : rules.FullManagementMinutes + ((minutes - rules.FullManagementMinutes) / rules.AdditionalBlockMinutes + 1) * rules.AdditionalBlockMinutes;

        var setupThisPeriod = input.SetupCompletedOn.HasValue && period.Contains(input.SetupCompletedOn.Value);

        var issues = new List<string>();
        if (needed > 0)
        {
            issues.Add(maxReachable < rules.FullSupplyMinDays
                ? $"Cannot reach {rules.FullSupplyMinDays} data days this period ({input.DataDays} so far, {openDays} days left)."
                : $"Needs {needed} more data day{Plural(needed)} in the next {openDays} day{Plural(openDays)}.");
        }
        if (!hasComm)
        {
            issues.Add("No real-time interactive communication logged this month.");
        }
        if (minutes < rules.FullManagementMinutes)
        {
            issues.Add($"{rules.FullManagementMinutes - minutes} more management minute{Plural(rules.FullManagementMinutes - minutes)} to reach {rules.FullManagementMinutes} this month.");
        }

        RtmReadiness readiness;
        if (!input.IsActive)
        {
            readiness = RtmReadiness.Inactive;
            issues.Insert(0, "Care plan is not active.");
        }
        else if (input.DocumentationStatus == DocumentationStatus.Signed)
        {
            readiness = RtmReadiness.Signed;
        }
        else if (deviceCode == RtmCodes.DeviceSupplyFull && managementCodes.Count > 0)
        {
            readiness = RtmReadiness.ReadyToSign;
        }
        else if (daysLeft <= 1 && deviceCode.Length > 0 && managementCodes.Count > 0)
        {
            readiness = RtmReadiness.ReadyToSign;
        }
        else if (maxReachable < rules.FullSupplyMinDays)
        {
            readiness = RtmReadiness.AtRisk;
        }
        else if (needed > 0 && needed > openDays * 0.8)
        {
            readiness = RtmReadiness.AtRisk;
        }
        else if (daysLeft <= 7 && !hasComm)
        {
            readiness = RtmReadiness.AtRisk;
        }
        else
        {
            readiness = RtmReadiness.OnTrack;
        }

        return new RtmAssessment
        {
            Period = period,
            DayOfPeriod = period.DayOfPeriod(input.Today),
            DaysLeft = daysLeft,
            DataDays = input.DataDays,
            DataDaysNeededForFullSupply = needed,
            MaxReachableDataDays = maxReachable,
            DeviceSupplyCode = deviceCode,
            ManagementMinutes = minutes,
            MinutesToNextThreshold = nextThreshold - minutes,
            InteractiveCommunications = input.InteractiveCommunications,
            ManagementCodes = managementCodes,
            SetupCompletedOn = input.SetupCompletedOn,
            SetupCompletedThisPeriod = setupThisPeriod,
            DocumentationStatus = input.DocumentationStatus,
            Readiness = readiness,
            Issues = readiness is RtmReadiness.Signed or RtmReadiness.ReadyToSign ? Array.Empty<string>() : issues
        };
    }

    public static IReadOnlyList<BillableCode> ManagementCodes(int minutes, bool hasInteractiveCommunication, RtmRules rules)
    {
        var codes = new List<BillableCode>();
        if (!hasInteractiveCommunication)
        {
            return codes;
        }
        if (minutes >= rules.FullManagementMinutes)
        {
            codes.Add(new BillableCode(RtmCodes.ManagementFull, 1));
            var additional = (minutes - rules.FullManagementMinutes) / rules.AdditionalBlockMinutes;
            if (additional > 0)
            {
                codes.Add(new BillableCode(RtmCodes.ManagementAdditional, additional));
            }
        }
        else if (minutes >= rules.BriefManagementMinutes)
        {
            codes.Add(new BillableCode(RtmCodes.ManagementBrief, 1));
        }
        return codes;
    }

    private static string Plural(int n) => n == 1 ? string.Empty : "s";
}
