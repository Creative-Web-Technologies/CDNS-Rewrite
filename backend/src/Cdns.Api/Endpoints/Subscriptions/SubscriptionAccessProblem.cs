using Cdns.Application.Feature.Subscriptions;

namespace Cdns.Api.Endpoints.Subscriptions;

/// <summary>
/// Maps a <see cref="SubscriptionAccessCode"/> failure to an HTTP result with a
/// stable <c>code</c> field that future CusDec endpoints reuse. Access failures
/// are 403; a missing subscription is 404. KeyMissing and KeyMismatch collapse to
/// a single "SubscriptionKeyInvalid" so the response never reveals whether a key
/// exists. Not wired into any endpoint yet — it will gate CusDec data endpoints.
/// </summary>
public static class SubscriptionAccessProblem
{
    /// <summary>The stable, client-facing code string for a failure.</summary>
    public static string CodeString(SubscriptionAccessCode code) => code switch
    {
        SubscriptionAccessCode.NotFound        => "SubscriptionNotFound",
        SubscriptionAccessCode.NotOwner        => "SubscriptionNotOwned",
        SubscriptionAccessCode.Inactive        => "SubscriptionInactive",
        SubscriptionAccessCode.Expired         => "SubscriptionExpired",
        SubscriptionAccessCode.CompanyInactive => "CompanyInactive",
        SubscriptionAccessCode.KeyMissing          => "SubscriptionKeyInvalid",
        SubscriptionAccessCode.KeyMismatch         => "SubscriptionKeyInvalid",
        SubscriptionAccessCode.AcknowledgementOnly => "SubscriptionAcknowledgementOnly",
        SubscriptionAccessCode.Ok                  => "Ok",
        _ => "SubscriptionAccessDenied",
    };

    private static int StatusCode(SubscriptionAccessCode code) =>
        code == SubscriptionAccessCode.NotFound
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status403Forbidden;

    /// <summary>
    /// Produces the ProblemDetails result for a non-Ok access code. Throws if
    /// called with <see cref="SubscriptionAccessCode.Ok"/> (there is no problem to
    /// report).
    /// </summary>
    public static IResult ToResult(SubscriptionAccessCode code)
    {
        if (code == SubscriptionAccessCode.Ok)
            throw new InvalidOperationException("Ok is not a failure; nothing to map.");

        var status = StatusCode(code);
        return Results.Problem(
            statusCode: status,
            title: status == StatusCodes.Status404NotFound
                ? "Subscription not found."
                : "Subscription access denied.",
            extensions: new Dictionary<string, object?> { ["code"] = CodeString(code) });
    }
}
