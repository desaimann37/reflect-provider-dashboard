using System.Globalization;
using Reflect.Domain.Clinical;
using Reflect.Domain.Common;
using Reflect.Domain.Rules;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.PatientDetails;

public class RtmPanelViewModel
{
    public string PeriodLabel { get; set; } = string.Empty;
    public string PeriodStartIso { get; set; } = string.Empty;
    public string DayOfPeriodLabel { get; set; } = string.Empty;
    public string MonthLabel { get; set; } = string.Empty;
    public BadgeViewModel Readiness { get; set; }
    public List<string> Issues { get; set; } = new();
    public List<RtmMetricRowViewModel> Metrics { get; set; } = new();
    public List<RtmCodeRowViewModel> Codes { get; set; } = new();
    public List<TimeEntryRowViewModel> TimeEntries { get; set; } = new();
    public List<PeriodHistoryRowViewModel> History { get; set; } = new();
    public bool CanSignOff { get; set; }
    public bool IsSigned { get; set; }
    public string SignOffWarning { get; set; } = string.Empty;
    public string SignOffSummary { get; set; } = string.Empty;

    public static RtmPanelViewModel FromSummary(PatientMonitoringSummary summary, RtmRules rules)
    {
        var rtm = summary.Rtm;
        var metrics = summary.PeriodMetrics;
        var month = RtmCalculator.CalendarMonth(summary.Today);
        var monthName = summary.Today.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

        var panel = new RtmPanelViewModel
        {
            PeriodLabel = ProviderUi.PeriodLabel(rtm.Period),
            PeriodStartIso = rtm.Period.Start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DayOfPeriodLabel = $"Day {rtm.DayOfPeriod} of {rtm.Period.LengthDays} · {rtm.DaysLeft} day{(rtm.DaysLeft == 1 ? string.Empty : "s")} left",
            MonthLabel = monthName,
            Readiness = ProviderUi.Readiness(rtm.Readiness),
            Issues = rtm.Issues.ToList(),
            IsSigned = rtm.Readiness == RtmReadiness.Signed,
            CanSignOff = rtm.Readiness is not (RtmReadiness.Signed or RtmReadiness.Inactive) && rtm.DataDays >= rules.ShortSupplyMinDays,
            Metrics = new List<RtmMetricRowViewModel>
            {
                new("Active / qualifying data days", $"{metrics.QualifyingDataDays} ({rules.FullSupplyMinDays} needed)", "Days with a completed session or patient-reported entry, this 30-day period", metrics.QualifyingDataDays >= rules.FullSupplyMinDays),
                new("Sessions", metrics.Sessions.ToString(CultureInfo.InvariantCulture), "Reflect XR and Reflect Mobile sessions this period", null),
                new("Total engagement", ProviderUi.Minutes(metrics.EngagementMinutes), "Time in app sessions this period", null),
                new("Therapeutic-response entries", metrics.TherapeuticResponseEntries.ToString(CultureInfo.InvariantCulture), "Mood check-ins, PHQ-9 and GAD-7 this period", null),
                new("Provider reviews", metrics.ProviderReviews.ToString(CultureInfo.InvariantCulture), "Logged data reviews this period", null),
                new("Patient communications", metrics.PatientCommunications.ToString(CultureInfo.InvariantCulture), $"Real-time interactive communications in {monthName}", metrics.PatientCommunications > 0),
                new("Provider management time", ProviderUi.Minutes(metrics.ManagementMinutes), $"Treatment-management minutes in {monthName}", metrics.ManagementMinutes >= rules.FullManagementMinutes),
                new("Documentation status", ProviderUi.Documentation(metrics.DocumentationStatus).Text, "Provider sign-off for this period", metrics.DocumentationStatus == DocumentationStatus.Signed)
            },
            Codes = BuildCodes(rtm, rules, monthName),
            TimeEntries = summary.TimeEntries
                .Where(t => t.ServiceDate >= month.Start.AddMonths(-1))
                .OrderByDescending(t => t.ServiceDate)
                .Select(TimeEntryRowViewModel.FromEntity)
                .ToList(),
            History = summary.Reviews
                .Where(r => r.PeriodStart != rtm.Period.Start)
                .OrderByDescending(r => r.PeriodStart)
                .Select(PeriodHistoryRowViewModel.FromEntity)
                .ToList()
        };

        panel.SignOffSummary = rtm.EligibleCodes.Count == 0
            ? "No codes are supported by the current numbers."
            : "Codes supported by the current numbers: " + string.Join(", ", rtm.EligibleCodes.Select(c => c.Label)) + ".";
        if (rtm.Readiness == RtmReadiness.AtRisk || rtm.Readiness == RtmReadiness.OnTrack)
        {
            panel.SignOffWarning = rtm.Issues.Count > 0 ? "Not all thresholds are met yet: " + string.Join(" ", rtm.Issues) : string.Empty;
        }
        return panel;
    }

    private static List<RtmCodeRowViewModel> BuildCodes(RtmAssessment rtm, RtmRules rules, string monthName)
    {
        var codes = new List<RtmCodeRowViewModel>
        {
            new(RtmCodes.Setup, RtmCodes.Describe(RtmCodes.Setup),
                rtm.SetupCompletedOn.HasValue ? $"Completed {rtm.SetupCompletedOn.Value.ToUsDateString()}" : "Not documented",
                rtm.SetupCompletedThisPeriod ? Met.Yes : rtm.SetupCompletedOn.HasValue ? Met.NotApplicable : Met.No,
                rtm.SetupCompletedThisPeriod ? "Reportable once per episode, this period." : rtm.SetupCompletedOn.HasValue ? "Reported in an earlier period." : "Record the setup date on the care plan."),
            new(rtm.DataDays >= rules.FullSupplyMinDays ? RtmCodes.DeviceSupplyFull : RtmCodes.DeviceSupplyShort,
                RtmCodes.Describe(rtm.DataDays >= rules.FullSupplyMinDays ? RtmCodes.DeviceSupplyFull : RtmCodes.DeviceSupplyShort),
                $"{rtm.DataDays} data day{(rtm.DataDays == 1 ? string.Empty : "s")} this period",
                rtm.DataDays >= rules.FullSupplyMinDays ? Met.Yes : rtm.DataDays >= rules.ShortSupplyMinDays ? Met.Partial : Met.No,
                rtm.DataDays >= rules.FullSupplyMinDays
                    ? "16-30 day threshold met."
                    : rtm.DataDays >= rules.ShortSupplyMinDays
                        ? $"2-15 day code supported; {rtm.DataDaysNeededForFullSupply} more for 98978."
                        : $"{rules.ShortSupplyMinDays - rtm.DataDays} more for 98986."),
            new(rtm.ManagementMinutes >= rules.FullManagementMinutes ? RtmCodes.ManagementFull : RtmCodes.ManagementBrief,
                RtmCodes.Describe(rtm.ManagementMinutes >= rules.FullManagementMinutes ? RtmCodes.ManagementFull : RtmCodes.ManagementBrief),
                $"{rtm.ManagementMinutes} min in {monthName}, {(rtm.HasInteractiveCommunication ? "live call logged" : "no live call yet")}",
                rtm.ManagementCodes.Count > 0 ? (rtm.ManagementMinutes >= rules.FullManagementMinutes ? Met.Yes : Met.Partial) : Met.No,
                !rtm.HasInteractiveCommunication
                    ? "Needs a real-time interactive communication this month."
                    : rtm.ManagementMinutes >= rules.FullManagementMinutes
                        ? $"{rtm.MinutesToNextThreshold} more minutes for an additional 98981."
                        : $"{rtm.MinutesToNextThreshold} more minutes to reach the next threshold.")
        };
        var additional = rtm.ManagementCodes.FirstOrDefault(c => c.Code == RtmCodes.ManagementAdditional);
        if (additional != null)
        {
            codes.Add(new(RtmCodes.ManagementAdditional, RtmCodes.Describe(RtmCodes.ManagementAdditional),
                $"{additional.Units} additional block{(additional.Units == 1 ? string.Empty : "s")}", Met.Yes, "Each full 20 minutes beyond the first 20."));
        }
        return codes;
    }
}

public enum Met
{
    No = 0,
    Partial = 1,
    Yes = 2,
    NotApplicable = 3
}

public sealed record RtmMetricRowViewModel(string Metric, string Value, string Help, bool? Met);

public sealed record RtmCodeRowViewModel(string Code, string Description, string Evidence, Met Status, string Note)
{
    public BadgeViewModel Badge => Status switch
    {
        Met.Yes => new("success", "Met", "ri-checkbox-circle-line"),
        Met.Partial => new("warning", "Partly met", "ri-error-warning-line"),
        Met.NotApplicable => new("info", "Done earlier", "ri-history-line"),
        _ => new("danger", "Not met", "ri-close-circle-line")
    };
}
