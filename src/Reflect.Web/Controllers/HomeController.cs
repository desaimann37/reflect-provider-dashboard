using Microsoft.AspNetCore.Mvc;
using Reflect.Domain.Common;

namespace Reflect.Web.Controllers;

/// <summary>
/// STAND-IN for the real root controller: "/" sends signed-in users to their role's dashboard,
/// and the error actions render the stand-in error pages.
/// </summary>
public class HomeController : Controller
{
    private readonly ICurrentUserService _currentUser;

    public HomeController(ICurrentUserService currentUser) => _currentUser = currentUser;

    public IActionResult Index() =>
        _currentUser.IsInRole(ProviderRoles.Provider)
            ? RedirectToAction("Dashboard", "Home", new { area = "Provider" })
            : RedirectToAction("AccessDenied", "Account");

    [Route("/Home/Status/{code:int}")]
    public IActionResult Status(int code)
    {
        Response.StatusCode = code;
        return code == 404 ? View("NotFound") : View("Error");
    }

    public IActionResult Error() => View();
}
