using Microsoft.AspNetCore.Mvc.Rendering;
using Reflect.Domain.Common;
using Reflect.Domain.Rules;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.Caseload;

public class CaseloadViewModel
{
    public const string FilterAll = "all";
    public const string FilterAttention = "attention";
    public const string FilterAtRisk = "atrisk";
    public const string FilterReady = "ready";

    public string Filter { get; set; } = FilterAll;
    public int TotalCount { get; set; }
    public List<CaseloadRowViewModel> Rows { get; set; } = new();

    public List<SelectListItem> FilterOptions => new()
    {
        new SelectListItem("All patients", FilterAll, Filter == FilterAll),
        new SelectListItem("Needs attention", FilterAttention, Filter == FilterAttention),
        new SelectListItem("RTM at risk", FilterAtRisk, Filter == FilterAtRisk),
        new SelectListItem("RTM ready to sign", FilterReady, Filter == FilterReady)
    };

    public async Task PrepareData(ProviderRepositories repos, ICurrentUserService currentUser, DateTimeOffset now, RtmRules rules, string filter, CancellationToken ct)
    {
        Filter = filter is FilterAttention or FilterAtRisk or FilterReady ? filter : FilterAll;
        var provider = await CaseloadDataLoader.GetCurrentProviderAsync(repos, currentUser, ct);
        var caseload = await CaseloadDataLoader.LoadCaseloadAsync(repos, provider, now, rules, ct);
        TotalCount = caseload.Count;

        IEnumerable<PatientMonitoringSummary> rows = Filter switch
        {
            FilterAttention => caseload.Where(s => s.NeedsAttention && s.Rtm.Readiness != RtmReadiness.Inactive),
            FilterAtRisk => caseload.Where(s => s.Rtm.Readiness == RtmReadiness.AtRisk),
            FilterReady => caseload.Where(s => s.Rtm.Readiness == RtmReadiness.ReadyToSign),
            _ => caseload
        };

        Rows = rows
            .OrderBy(s => s.Rtm.Readiness == RtmReadiness.Inactive)
            .ThenBy(s => s.TopSeverity ?? InsightSeverity.Positive)
            .ThenBy(s => s.Patient.LastName)
            .Select(s => CaseloadRowViewModel.FromSummary(s, now))
            .ToList();
    }
}
