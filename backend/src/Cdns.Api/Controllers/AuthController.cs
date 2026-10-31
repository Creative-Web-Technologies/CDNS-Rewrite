using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Cdns.Api.Configuration;
using Cdns.Api.Middleware;
using Cdns.Infrastructure.Persistence;
using Cdns.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Cdns.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    CdnsDbContext db,
    IPasswordHasher passwordHasher,
    JwtSettings jwtSettings,
    LoginAttemptTracker loginAttemptTracker) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var matchingUsers = await db.User
            .Include(candidate => candidate.Employee)
            .Where(candidate => candidate.Employee.Email != null
                && candidate.Employee.Email.ToLower() == normalizedEmail)
            .OrderBy(candidate => candidate.EmployeeId)
            .Take(2)
            .ToListAsync();
        var user = matchingUsers.Count == 1 ? matchingUsers[0] : null;

        if (user?.Password is null
            || string.IsNullOrWhiteSpace(user.PasswordSalt)
            || !passwordHasher.Verify(request.Password, user.Password, user.PasswordSalt))
        {
            loginAttemptTracker.RecordFailure(GetClientIpAddress());
            return Unauthorized(new { message = "Invalid credentials" });
        }

        if (!user.IsActive || user.IsSuspended || user.Employee.IsDeleted)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "This account is inactive or locked."
            });
        }

        var roles = await (
            from mapping in db.UserGroupMappings
            join roleGroup in db.Group on mapping.GroupId equals roleGroup.GroupId
            where mapping.EmployeeId == user.EmployeeId
                && !roleGroup.IsDeleted
                && roleGroup.GroupName != null
            select roleGroup.GroupName!)
            .Distinct()
            .ToListAsync();

        if (roles.Count == 0)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "This account has no active role assigned."
            });
        }

        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(jwtSettings.ExpiryMinutes);
        var claims = new List<Claim>
        {
            new("sub", user.EmployeeId.ToString()),
            new("email", user.Employee.Email!)
        };
        claims.AddRange(roles.Select(role => new Claim("role", role)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret));
        var token = new JwtSecurityToken(
            issuer: jwtSettings.Issuer,
            audience: jwtSettings.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expiresAt));
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        return Ok(new
        {
            id = User.FindFirstValue("sub"),
            email = User.FindFirstValue("email"),
            roles = User.FindAll("role").Select(claim => claim.Value).ToArray()
        });
    }

    private string GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed record LoginResponse(string Token, DateTimeOffset ExpiresAt);
