using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Cdns.Api.Filters;

/// <summary>
/// Rejects any request whose query string contains a null character (\x00) in a
/// parameter name or value with 400 ProblemDetails / code "InvalidRequest".
/// Registered globally so the check covers every controller action.
/// Action filters run after UseAuthentication and UseAuthorization, so an
/// unauthenticated request to a protected endpoint still returns 401.
/// </summary>
public sealed class NullCharacterQueryStringFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var key in context.HttpContext.Request.Query.Keys)
        {
            if (ContainsNull(key))
            {
                Reject(context);
                return;
            }

            foreach (var value in context.HttpContext.Request.Query[key])
            {
                if (ContainsNull(value))
                {
                    Reject(context);
                    return;
                }
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }

    private static bool ContainsNull(string? s) => s is not null && s.Contains('\0');

    private static void Reject(ActionExecutingContext context)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid request.",
        };
        problem.Extensions["code"] = "InvalidRequest";
        context.Result = new ObjectResult(problem) { StatusCode = StatusCodes.Status400BadRequest };
    }
}
