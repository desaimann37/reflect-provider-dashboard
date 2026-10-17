using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.Domain.Common;
using Reflect.Domain.Rules;
using Reflect.Web.Models.ViewModels.Provider.Dashboard;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize(Policy = ProviderPolicies.ProviderOnly)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class HomeController : Controller
{
    private readonly ProviderRepositories _repos;
    private readonly ICurrentUserService _currentUser;
    private readonly TimeProvider _clock;
    private readonly RtmRules _rules;

    public HomeController(ProviderRepositories repos, ICurrentUserService currentUser, TimeProvider clock, RtmRules rules)
    {
        _repos = repos;
        _currentUser = currentUser;
        _clock = clock;
        _rules = rules;
    }

    public IActionResult Index() => RedirectToAction(nameof(Dashboard));

    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var vm = new ProviderDashboardViewModel();
        await vm.PrepareData(_repos, _currentUser, _clock.GetUtcNow(), _rules, ct);
        return View(vm);
    }
}
