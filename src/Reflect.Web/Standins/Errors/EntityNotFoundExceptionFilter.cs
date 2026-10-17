using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Reflect.Domain.Common;

namespace Reflect.Web.Standins.Errors;

/// <summary>STAND-IN. Maps EntityNotFoundException to a 404 page (MVC) or a JSON error body (AJAX).</summary>
public sealed class EntityNotFoundExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not EntityNotFoundException ex)
        {
            return;
        }

        var request = context.HttpContext.Request;
        var wantsJson = request.Headers.XRequestedWith == "XMLHttpRequest"
            || request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);

        context.Result = wantsJson
            ? new JsonResult(new { error = "The requested record was not found." }) { StatusCode = 404 }
            : new ViewResult { ViewName = "NotFound", StatusCode = 404 };
        context.ExceptionHandled = true;
        _ = ex;
    }
}
