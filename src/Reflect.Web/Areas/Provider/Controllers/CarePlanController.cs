using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.Domain.Common;
using Reflect.Web.Models.Messages;
using Reflect.Web.Models.ViewModels.Provider.EditCarePlan;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Areas.Provider.Controllers;

/// <summary>Edit page for the care plan. The read-only view lives on the patient's Care plan tab.</summary>
[Area("Provider")]
[Authorize(Policy = ProviderPolicies.ProviderOnly)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class CarePlanController : Controller
{
    private readonly ProviderRepositories _repos;
    private readonly ICurrentUserService _currentUser;
    private readonly TimeProvider _clock;

    public CarePlanController(ProviderRepositories repos, ICurrentUserService currentUser, TimeProvider clock)
    {
        _repos = repos;
        _currentUser = currentUser;
        _clock = clock;
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var vm = new EditCarePlanViewModel();
        await vm.PrepareData(_repos, _currentUser, id, _clock.GetUtcNow(), loadValues: true, ct);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditCarePlanViewModel model, CancellationToken ct)
    {
        var now = _clock.GetUtcNow();
        var saveData = await model.PrepareDataForSave(_repos, _currentUser, now, ModelState, ct);
        if (saveData == null)
        {
            await model.PrepareData(_repos, _currentUser, model.PatientId, now, loadValues: false, ct);
            return View(model);
        }

        await _repos.CarePlans.UpdateAsync(saveData.CarePlan, ct);
        TempData[ProviderMessages.TempDataMessageKey] = ProviderMessages.CarePlanUpdated(saveData.PatientName);
        TempData[ProviderMessages.TempDataTypeKey] = "success";
        return RedirectToAction("Details", "Patients", new { id = model.PatientId, tab = "careplan" });
    }
}
