using Cdns.Application.Auth;

namespace Cdns.Api.Endpoints.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth");

        auth.MapPost("/login", async (LoginRequest req, ILoginService loginService, CancellationToken ct) =>
        {
            var result = await loginService.LoginAsync(req.UserName, req.Password, ct);
            return result switch
            {
                LoginResult.Success =>
                    Results.Ok(new { type = "success" }),
                LoginResult.MasterActivationRequired m =>
                    Results.Ok(new { type = "masterActivationRequired", activationToken = m.ActivationToken }),
                LoginResult.AccountLocked =>
                    Results.Json(new { code = "AccountLocked" }, statusCode: 401),
                LoginResult.InvalidCredentials =>
                    Results.Json(new { code = "InvalidCredentials" }, statusCode: 401),
                _ => Results.StatusCode(500)
            };
        });

        var activation = auth.MapGroup("/master-activation");

        activation.MapPost("/profile", async (
            CompleteProfileHttpRequest req,
            IMasterActivationService svc,
            CancellationToken ct) =>
        {
            var result = await svc.CompleteProfileAsync(
                req.ActivationToken,
                new CompleteProfileRequest(req.FirstName, req.LastName, req.Mobile, req.Nic, req.Designation),
                ct);
            return result switch
            {
                CompleteProfileResult.Success => Results.Ok(),
                CompleteProfileResult.InvalidToken => Results.NotFound(new { code = "InvalidActivationToken" }),
                _ => Results.StatusCode(500)
            };
        });

        activation.MapPost("/password", async (
            SetActivationPasswordHttpRequest req,
            IMasterActivationService svc,
            CancellationToken ct) =>
        {
            var result = await svc.SetPasswordAsync(req.ActivationToken, req.Password, ct);
            return result switch
            {
                SetActivationPasswordResult.Success =>
                    Results.Ok(new { type = "success" }),
                SetActivationPasswordResult.InvalidToken =>
                    Results.NotFound(new { code = "InvalidActivationToken" }),
                _ => Results.StatusCode(500)
            };
        });

        var reset = auth.MapGroup("/password-reset");

        // Always 200 — caller must not be able to enumerate valid usernames
        reset.MapPost("/forgot-password", async (
            ForgotPasswordHttpRequest req,
            IPasswordResetService svc,
            CancellationToken ct) =>
        {
            await svc.SendOtpAsync(req.UserName, ct);
            return Results.Ok(new { sent = true });
        });

        reset.MapPost("/verify-otp", async (
            VerifyOtpHttpRequest req,
            IPasswordResetService svc,
            CancellationToken ct) =>
        {
            var result = await svc.VerifyOtpAsync(req.UserName, req.Otp, ct);
            return result switch
            {
                VerifyOtpResult.Valid => Results.Ok(),
                VerifyOtpResult.Invalid => Results.BadRequest(new { code = "InvalidOtp" }),
                _ => Results.StatusCode(500)
            };
        });

        reset.MapPost("/reset-password", async (
            ResetPasswordHttpRequest req,
            IPasswordResetService svc,
            CancellationToken ct) =>
        {
            var result = await svc.ResetPasswordAsync(req.UserName, req.Otp, req.NewPassword, ct);
            return result switch
            {
                ResetPasswordResult.Success => Results.Ok(),
                // Collapse InvalidOtp and UserNotFound — same code to avoid enumeration
                _ => Results.BadRequest(new { code = "InvalidOtp" })
            };
        });

        return app;
    }
}
