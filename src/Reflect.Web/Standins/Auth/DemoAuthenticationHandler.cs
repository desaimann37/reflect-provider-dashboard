using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Reflect.Web.Standins.Auth;

/// <summary>
/// STAND-IN for Entra External ID. Signs every request in as the demo user named in the
/// <c>rx_demo_user</c> cookie (default: the first provider).
/// </summary>
public sealed class DemoAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "DemoAuth";
    public const string CookieName = "rx_demo_user";
    public const string EntraObjectIdClaim = "oid";

    public DemoAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var user = DemoUsers.Find(Request.Cookies[CookieName]) ?? DemoUsers.Okafor;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(EntraObjectIdClaim, user.EntraObjectId),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName)
        };
        claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r)));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.Redirect("/Account/AccessDenied");
        return Task.CompletedTask;
    }
}
