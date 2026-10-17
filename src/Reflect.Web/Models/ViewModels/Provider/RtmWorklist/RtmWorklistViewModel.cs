using Microsoft.AspNetCore.Mvc.Rendering;
using Reflect.Domain.Common;
using Reflect.Domain.Rules;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.RtmWorklist;

public class RtmWorklistViewModel
{
    public string Filter { get; set; } = "open";
    public string MonthLabel { get; set; } = string.Empty;
    public int ReadyCount { get; set; }
    public int AtRiskCount { get; set; }
    public int OnTrackCount { get; set; }
    public int SignedCount { get; set; }
    public int TotalMinutesThisMonth { get; set; }
    public List<RtmWorklistRowViewModel> Rows { get; set; } = new();

    public List<SelectListItem> FilterOptions => new()
    {
        new SelectListItem("Open periods", "open", Filter == "open"),
        new SelectListItem("Ready to sign", "ready", Filter == "ready"),
        new SelectListItem("At risk", "atrisk", Filter == "atrisk"),
        new SelectListItem("On track", "ontrack", Filter == "ontrack"),
        new SelectListItem("Signed", "signed", Filter == "signed"),
        new SelectListItem("All, including paused", "all", Filter == "all")
    };

    public async Task PrepareData(ProviderRepositories repos, ICurrentUserService currentUser, DateTimeOffset now, RtmRules rules, string filter, CancellationToken ct)
    {
        Filter = filter is "ready" or "atrisk" or "ontrack" or "signed" or "all" ? filter : "open";
        var provider = await CaseloadDataLoader.GetCurrentProviderAsync(repos, currentUser, ct);
        var caseload = await CaseloadDataLoader.LoadCaseloadAsync(repos, provider, now, rules, ct);
        MonthLabel = now.ToEasternDate().ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);

        ReadyCount = caseload.Count(s => s.Rtm.Readiness == RtmReadiness.ReadyToSign);
        AtRiskCount = caseload.Count(s => s.Rtm.Readiness == RtmReadiness.AtRisk);
        OnTrackCount = caseload.Count(s => s.Rtm.Readiness == RtmReadiness.OnTrack);
        SignedCount = caseload.Count(s => s.Rtm.Readiness == RtmReadiness.Signed);
        TotalMinutesThisMonth = caseload.Sum(s => s.Rtm.ManagementMinutes);

        IEnumerable<PatientMonitoringSummary> rows = Filter switch
        {
            "ready" => caseload.Where(s => s.Rtm.Readiness == RtmReadiness.ReadyToSign),
            "atrisk" => caseload.Where(s => s.Rtm.Readiness == RtmReadiness.AtRisk),
            "ontrack" => caseload.Where(s => s.Rtm.Readiness == RtmReadiness.OnTrack),
            "signed" => caseload.Where(s => s.Rtm.Readiness == RtmReadiness.Signed),
            "all" => caseload,
            _ => caseload.Where(s => s.Rtm.Readiness is RtmReadiness.ReadyToSign or RtmReadiness.AtRisk or RtmReadiness.OnTrack)
        };

        Rows = rows
            .OrderBy(s => s.Rtm.Readiness == RtmReadiness.Inactive)
            .ThenBy(s => s.Rtm.Period.End)
            .ThenBy(s => s.Patient.LastName)
            .Select(RtmWorklistRowViewModel.FromSummary)
            .ToList();
    }
}
