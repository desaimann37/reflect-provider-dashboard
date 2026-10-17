using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.Domain.Common;
using Reflect.Web.Models.Messages;
using Reflect.Web.Models.ViewModels.Provider.AddNote;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize(Policy = ProviderPolicies.ProviderOnly)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class NotesController : Controller
{
    private readonly ProviderRepositories _repos;
    private readonly ICurrentUserService _currentUser;
    private readonly TimeProvider _clock;

    public NotesController(ProviderRepositories repos, ICurrentUserService currentUser, TimeProvider clock)
    {
        _repos = repos;
        _currentUser = currentUser;
        _clock = clock;
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid patientId, CancellationToken ct)
    {
        var vm = new AddNoteViewModel();
        await vm.PrepareData(_repos, _currentUser, patientId, ct);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AddNoteViewModel model, CancellationToken ct)
    {
        var saveData = await model.PrepareDataForSave(_repos, _currentUser, _clock.GetUtcNow(), ModelState, ct);
        if (saveData == null)
        {
            await model.PrepareData(_repos, _currentUser, model.PatientId, ct);
            return View(model);
        }

        await _repos.Notes.AddAsync(saveData.Note, ct);
        TempData[ProviderMessages.TempDataMessageKey] = ProviderMessages.NoteAdded(saveData.PatientName);
        TempData[ProviderMessages.TempDataTypeKey] = "success";
        return RedirectToAction("Details", "Patients", new { id = model.PatientId, tab = "notes" });
    }
}
