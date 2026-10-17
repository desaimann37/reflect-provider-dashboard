using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.Domain.Common;
using Reflect.Domain.Rules;
using Reflect.Web.Models.Messages;
using Reflect.Web.Models.ViewModels.Provider.LogTime;
using Reflect.Web.Models.ViewModels.Provider.RtmWorklist;
using Reflect.Web.Models.ViewModels.Provider.Shared;
using Reflect.Web.Models.ViewModels.Provider.SignOff;

namespace Reflect.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize(Policy = ProviderPolicies.ProviderOnly)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class RtmController : Controller
{
    private readonly ProviderRepositories _repos;
    private readonly ICurrentUserService _currentUser;
    private readonly TimeProvider _clock;
    private readonly RtmRules _rules;

    public RtmController(ProviderRepositories repos, ICurrentUserService currentUser, TimeProvider clock, RtmRules rules)
    {
        _repos = repos;
        _currentUser = currentUser;
        _clock = clock;
        _rules = rules;
    }

    public async Task<IActionResult> Index(string filter, CancellationToken ct)
    {
        var vm = new RtmWorklistViewModel();
        await vm.PrepareData(_repos, _currentUser, _clock.GetUtcNow(), _rules, filter, ct);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> LogTime(Guid patientId, CancellationToken ct)
    {
        var vm = new LogTimeViewModel();
        await vm.PrepareData(_repos, _currentUser, patientId, _clock.GetUtcNow(), _rules, ct);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogTime(LogTimeViewModel model, CancellationToken ct)
    {
        var now = _clock.GetUtcNow();
        var saveData = await model.PrepareDataForSave(_repos, _currentUser, now, ModelState, ct);
        if (saveData == null)
        {
            await model.PrepareData(_repos, _currentUser, model.PatientId, now, _rules, ct);
            return View(model);
        }

        await _repos.TimeEntries.AddAsync(saveData.Entry, ct);
        TempData[ProviderMessages.TempDataMessageKey] = ProviderMessages.TimeLogged(saveData.Entry.Minutes, saveData.PatientName);
        TempData[ProviderMessages.TempDataTypeKey] = "success";
        return RedirectToAction("Details", "Patients", new { id = model.PatientId, tab = "rtm" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignOff(RtmSignOffViewModel model, CancellationToken ct)
    {
        var saveData = await model.PrepareDataForSave(_repos, _currentUser, _clock.GetUtcNow(), _rules, ModelState, ct);
        if (saveData == null)
        {
            TempData[ProviderMessages.TempDataMessageKey] = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? ProviderMessages.FixHighlightedFields;
            TempData[ProviderMessages.TempDataTypeKey] = "error";
            return RedirectToAction("Details", "Patients", new { id = model.PatientId, tab = "rtm" });
        }

        if (saveData.IsNew)
        {
            await _repos.Reviews.AddAsync(saveData.Review, ct);
        }
        else
        {
            await _repos.Reviews.UpdateAsync(saveData.Review, ct);
        }
        TempData[ProviderMessages.TempDataMessageKey] = ProviderMessages.PeriodSigned(saveData.PeriodLabel, saveData.PatientName);
        TempData[ProviderMessages.TempDataTypeKey] = "success";
        return RedirectToAction("Details", "Patients", new { id = model.PatientId, tab = "rtm" });
    }
}
