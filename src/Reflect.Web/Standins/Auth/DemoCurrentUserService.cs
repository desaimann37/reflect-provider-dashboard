using System.Security.Claims;
using Reflect.Domain.Common;

namespace Reflect.Web.Standins.Auth;

/// <summary>STAND-IN implementation of the real app's ICurrentUserService contract.</summary>
public sealed class DemoCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public DemoCurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal Principal => _accessor.HttpContext?.User;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string EntraObjectId => Principal?.FindFirstValue(DemoAuthenticationHandler.EntraObjectIdClaim) ?? string.Empty;

    public string Email => Principal?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;
}
