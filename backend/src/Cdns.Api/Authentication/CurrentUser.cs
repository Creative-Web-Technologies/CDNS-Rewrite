using System.Security.Claims;
using Cdns.Application.Interfaces;
using Cdns.Domain.Enums;

namespace Cdns.Api.Authentication;

/// <summary>
/// <see cref="ICurrentUser"/> backed by the current request's claims.
/// Claims are issued by <see cref="SessionCookieAuthHandler"/>:
/// <c>NameIdentifier</c> = MemberId, <c>Name</c> = UserName, <c>MemberType</c> = member type.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated == true &&
        long.TryParse(Principal.FindFirstValue(ClaimTypes.NameIdentifier), out _);

    public long MemberId
    {
        get
        {
            var raw = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(raw, out var memberId))
                throw new InvalidOperationException(
                    "ICurrentUser.MemberId was accessed on an unauthenticated request. " +
                    "Only resolve the current user inside endpoints that require authorization.");
            return memberId;
        }
    }

    public string UserName =>
        Principal?.FindFirstValue(ClaimTypes.Name)
        ?? throw new InvalidOperationException(
            "ICurrentUser.UserName was accessed on an unauthenticated request. " +
            "Only resolve the current user inside endpoints that require authorization.");

    public MemberType MemberType
    {
        get
        {
            var raw = Principal?.FindFirstValue("MemberType");
            if (!Enum.TryParse<MemberType>(raw, ignoreCase: true, out var memberType))
                throw new InvalidOperationException(
                    "ICurrentUser.MemberType was accessed on an unauthenticated request, " +
                    "or the MemberType claim was missing/unrecognised. " +
                    "Only resolve the current user inside endpoints that require authorization.");
            return memberType;
        }
    }
}
