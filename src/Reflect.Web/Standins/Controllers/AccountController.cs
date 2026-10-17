using Microsoft.AspNetCore.Mvc;

namespace Reflect.Web.Standins.Controllers;

/// <summary>STAND-IN for the shared sign-in area's Access denied page.</summary>
public class AccountController : Controller
{
    public IActionResult AccessDenied() => View();
}
