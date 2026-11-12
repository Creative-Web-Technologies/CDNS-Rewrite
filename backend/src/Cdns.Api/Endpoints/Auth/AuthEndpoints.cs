using System.Security.Claims;
using Cdns.Application.Auth;
using Cdns.Domain.Enums;
using Cdns.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
                LoginResult.Success s =>
                    Results.Ok(new { type = "success", portal = s.Portal }),
                LoginResult.MasterActivationRequired m =>
                    Results.Ok(new { type = "masterActivationRequired", activationToken = m.ActivationToken }),
                LoginResult.AccountLocked =>
                    Results.Json(new { code = "AccountLocked" }, statusCode: 401),
                LoginResult.InvalidCredentials =>
                    Results.Json(new { code = "InvalidCredentials" }, statusCode: 401),
                _ => Results.StatusCode(500)
            };
        });

        // Returns the current user's profile; 401 if no valid session
        auth.MapGet("/me", async (ClaimsPrincipal principal, CdnsDbContext db, CancellationToken ct) =>
        {
            var memberIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(memberIdStr, out var memberId))
                return Results.Unauthorized();

            var memberUser = await db.MemberUsers
                .Include(u => u.Member)
                .FirstOrDefaultAsync(u => u.MemberId == memberId, ct);

            if (memberUser is null)
                return Results.Unauthorized();

            var portal = memberUser.MemberType == MemberType.CompanyMasterMember ? "Company" : "Member";

            return Results.Ok(new
            {
                userName = memberUser.UserName,
                firstName = memberUser.Member.FirstName,
                lastName = memberUser.Member.LastName,
                memberType = memberUser.MemberType.ToString(),
                portal
            });
        }).RequireAuthorization();

        // Invalidates the session row and clears the cookie
        auth.MapPost("/logout", async (HttpContext ctx, CdnsDbContext db, CancellationToken ct) =>
        {
            if (ctx.Request.Cookies.TryGetValue("session_id", out var rawSessionId) &&
                Guid.TryParse(rawSessionId, out var sessionId))
            {
                var session = await db.MemberLoginSessions
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId, ct);

                if (session is not null)
                {
                    db.MemberLoginSessions.Remove(session);
                    await db.SaveChangesAsync(ct);
                }
            }

            ctx.Response.Cookies.Delete("session_id");
            return Results.Ok();
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
                SetActivationPasswordResult.Success s =>
                    Results.Ok(new { type = "success", portal = s.Portal }),
                SetActivationPasswordResult.InvalidToken =>
                    Results.NotFound(new { code = "InvalidActivationToken" }),
                SetActivationPasswordResult.WeakPassword =>
                    Results.BadRequest(new { code = "WeakPassword" }),
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
                ResetPasswordResult.WeakPassword => Results.BadRequest(new { code = "WeakPassword" }),
                // Collapse InvalidOtp and UserNotFound — same code to avoid enumeration
                _ => Results.BadRequest(new { code = "InvalidOtp" })
            };
        });

        return app;
    }
}
