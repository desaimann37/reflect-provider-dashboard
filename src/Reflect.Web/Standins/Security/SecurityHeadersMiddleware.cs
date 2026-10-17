namespace Reflect.Web.Standins.Security;

/// <summary>
/// STAND-IN. Sends the real app's Content Security Policy. With <c>Csp:StrictNonce</c> (default true)
/// it sends the stricter policy the real app is moving to: scripts need a per-request nonce and
/// 'unsafe-inline' is dropped, so anything that would break after the change breaks here first.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private const string CurrentPolicy =
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; font-src 'self' data:; connect-src 'self' wss:; " +
        "frame-ancestors 'none'; base-uri 'self'; object-src 'none'; form-action 'self';";

    private readonly RequestDelegate _next;
    private readonly bool _strictNonce;

    public SecurityHeadersMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _strictNonce = configuration.GetValue("Csp:StrictNonce", true);
    }

    public Task Invoke(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.ContentSecurityPolicy = _strictNonce
                ? CurrentPolicy.Replace("script-src 'self' 'unsafe-inline';", $"script-src 'self' 'nonce-{context.GetCspNonce()}';")
                : CurrentPolicy;
            headers.XContentTypeOptions = "nosniff";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            return Task.CompletedTask;
        });
        return _next(context);
    }
}
