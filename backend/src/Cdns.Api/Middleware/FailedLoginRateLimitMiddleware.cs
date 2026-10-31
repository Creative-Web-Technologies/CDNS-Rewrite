namespace Cdns.Api.Middleware;

public sealed class FailedLoginRateLimitMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, LoginAttemptTracker tracker)
    {
        var requestPath = context.Request.Path.Value?.TrimEnd('/');
        if (HttpMethods.IsPost(context.Request.Method)
            && string.Equals(requestPath, "/api/auth/login", StringComparison.OrdinalIgnoreCase))
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (tracker.IsLimited(ipAddress))
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Too many failed login attempts. Try again later."
                });
                return;
            }
        }

        await next(context);
    }
}
