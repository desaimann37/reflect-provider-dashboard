using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.Domain.Common;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Rules;
using Reflect.Web.Models.ViewModels.Provider.Brief;
using Reflect.Web.Models.ViewModels.Provider.Caseload;
using Reflect.Web.Models.ViewModels.Provider.PatientDetails;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize(Policy = ProviderPolicies.ProviderOnly)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class PatientsController : Controller
{
    private readonly ProviderRepositories _repos;
    private readonly ICurrentUserService _currentUser;
    private readonly TimeProvider _clock;
    private readonly RtmRules _rules;
    private readonly IClinicalBriefGenerator _briefs;

    public PatientsController(ProviderRepositories repos, ICurrentUserService currentUser, TimeProvider clock, RtmRules rules, IClinicalBriefGenerator briefs)
    {
        _repos = repos;
        _currentUser = currentUser;
        _clock = clock;
        _rules = rules;
        _briefs = briefs;
    }

    public async Task<IActionResult> Index(string filter, CancellationToken ct)
    {
        var vm = new CaseloadViewModel();
        await vm.PrepareData(_repos, _currentUser, _clock.GetUtcNow(), _rules, filter, ct);
        return View(vm);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var now = _clock.GetUtcNow();
        var vm = new PatientDetailsViewModel();
        await vm.PrepareData(_repos, _currentUser, id, now, _rules, ct);

        var access = PhiAccessLogFactory.Create(vm.PatientId, vm.ViewerUserId, vm.ViewerName, PhiAccessLogFactory.ViewedRecord, Request.Path, now);
        await _repos.AccessLog.AddAsync(access, ct);
        vm.AddAccessRow(access);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> EngagementData(Guid id, int weeks, CancellationToken ct)
    {
        var vm = new EngagementChartViewModel();
        await vm.PrepareData(_repos, _currentUser, id, weeks, _clock.GetUtcNow(), _rules, ct);
        return Json(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Brief(Guid id, CancellationToken ct)
    {
        var now = _clock.GetUtcNow();
        var vm = new PatientBriefViewModel();
        await vm.PrepareData(_repos, _currentUser, id, now, _rules, ct);
        vm.SetBrief(await _briefs.GenerateAsync(vm.Input, ct), now);

        await _repos.AccessLog.AddAsync(
            PhiAccessLogFactory.Create(vm.PatientId, vm.ViewerUserId, vm.ViewerName, PhiAccessLogFactory.GeneratedBrief, Request.Path, now), ct);
        return Json(vm.ToResponse());
    }
}
