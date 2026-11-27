using Cdns.Api.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace Cdns.Infrastructure.Tests;

/// <summary>
/// Unit tests for <see cref="NullCharacterQueryStringFilter"/>.
///
/// Demonstrates fail-before-fix behaviour: before the filter existed, any request
/// with a null character in a query string value would reach EF/Npgsql, which then
/// threw a 22021 "invalid byte sequence" error from PostgreSQL. The filter class
/// also didn't exist, so these tests would not compile at all on the unfixed tree.
/// After the fix all rejection cases return 400 and all pass-through cases leave
/// context.Result null.
/// </summary>
public class NullCharacterQueryStringFilterTests
{
    private readonly NullCharacterQueryStringFilter _filter = new();

    // Builds an ActionExecutingContext whose HttpContext has the given raw query string.
    // %00 in the raw string URL-decodes to \0 in the parsed IQueryCollection.
    private static ActionExecutingContext BuildContext(string rawQueryString)
    {
        var httpContext = new DefaultHttpContext();
        if (!string.IsNullOrEmpty(rawQueryString))
            httpContext.Request.QueryString = new QueryString(rawQueryString);

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());
    }

    // ── Rejection cases ────────────────────────────────────────────────────────

    // Setting context.Result causes MVC to skip the action method entirely —
    // that is the framework's short-circuit contract for action filters.

    [Fact]
    public void NullAlone_InValue_Returns400AndShortCircuits()
    {
        var ctx = BuildContext("?search=%00");

        _filter.OnActionExecuting(ctx);

        AssertRejected(ctx);
    }

    [Fact]
    public void NullInsideText_InValue_Returns400AndShortCircuits()
    {
        var ctx = BuildContext("?search=abc%00def");

        _filter.OnActionExecuting(ctx);

        AssertRejected(ctx);
    }

    [Fact]
    public void NullInParameterName_Returns400AndShortCircuits()
    {
        // %00 in the key name: "ke\0y" = value
        var ctx = BuildContext("?ke%00y=value");

        _filter.OnActionExecuting(ctx);

        AssertRejected(ctx);
    }

    // ── Pass-through cases ─────────────────────────────────────────────────────

    [Fact]
    public void NormalText_PassesThrough()
    {
        var ctx = BuildContext("?search=hello+world&page=1");

        _filter.OnActionExecuting(ctx);

        Assert.Null(ctx.Result);
    }

    [Fact]
    public void SinhalaText_PassesThrough()
    {
        // "පිට" URL-encoded
        var ctx = BuildContext("?search=%E0%B6%B4%E0%B7%92%E0%B6%A7");

        _filter.OnActionExecuting(ctx);

        Assert.Null(ctx.Result);
    }

    [Fact]
    public void Emoji_PassesThrough()
    {
        // 😀 URL-encoded
        var ctx = BuildContext("?search=%F0%9F%98%80");

        _filter.OnActionExecuting(ctx);

        Assert.Null(ctx.Result);
    }

    [Fact]
    public void PercentSign_PassesThrough()
    {
        // A literal % in a value (%25 URL-encodes to %)
        var ctx = BuildContext("?search=abc%25def");

        _filter.OnActionExecuting(ctx);

        Assert.Null(ctx.Result);
    }

    [Fact]
    public void Underscore_PassesThrough()
    {
        var ctx = BuildContext("?search=abc_def");

        _filter.OnActionExecuting(ctx);

        Assert.Null(ctx.Result);
    }

    [Fact]
    public void EmptyQueryString_PassesThrough()
    {
        var ctx = BuildContext("");

        _filter.OnActionExecuting(ctx);

        Assert.Null(ctx.Result);
    }

    // ── Signed-out + null character → 401 ─────────────────────────────────────

    // This case cannot be tested in the current test setup because verifying that
    // authorization fires BEFORE the action filter requires a full HTTP test host
    // (WebApplicationFactory<Program>) with a wired auth scheme and a request pipeline.
    // The project has no such integration-test harness. The architectural guarantee
    // is that UseAuthorization() runs in the middleware pipeline before MVC ever
    // dispatches to action filters, so an unauthenticated request is rejected with
    // 401 before the filter has a chance to run.

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static void AssertRejected(ActionExecutingContext ctx)
    {
        var result = Assert.IsType<ObjectResult>(ctx.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal("InvalidRequest", problem.Extensions["code"]?.ToString());
        Assert.False(problem.Extensions.ContainsKey("input"));
        Assert.False(problem.Extensions.ContainsKey("detail"));
    }
}
