using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.Domain.Common;
using Reflect.Domain.Rules;
using Reflect.Web.Models.ViewModels.Provider.Outcomes;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize(Policy = ProviderPolicies.ProviderOnly)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ReportsController : Controller
{
    private readonly ProviderRepositories _repos;
    private readonly ICurrentUserService _currentUser;
    private readonly TimeProvider _clock;
    private readonly RtmRules _rules;

    public ReportsController(ProviderRepositories repos, ICurrentUserService currentUser, TimeProvider clock, RtmRules rules)
    {
        _repos = repos;
        _currentUser = currentUser;
        _clock = clock;
        _rules = rules;
    }

    public async Task<IActionResult> Outcomes(CancellationToken ct)
    {
        var vm = new OutcomesReportViewModel();
        await vm.PrepareData(_repos, _currentUser, _clock.GetUtcNow(), _rules, ct);
        return View(vm);
    }
}
