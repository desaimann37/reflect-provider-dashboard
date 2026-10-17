using Microsoft.AspNetCore.Mvc;
using Reflect.Web.Standins.Auth;

namespace Reflect.Web.Standins.Controllers;

/// <summary>STAND-IN. Switches the fake signed-in user from the header menu.</summary>
public class DemoUserController : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Switch(string userId, string returnUrl)
    {
        var user = DemoUsers.Find(userId) ?? DemoUsers.Okafor;
        Response.Cookies.Append(DemoAuthenticationHandler.CookieName, user.UserId.ToString(), new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            IsEssential = true,
            Secure = Request.IsHttps
        });
        TempData["ToastMessage"] = $"Signed in as {user.DisplayName} (demo).";
        TempData["ToastType"] = "info";
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
    }
}
