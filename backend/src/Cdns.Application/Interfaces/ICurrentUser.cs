using Cdns.Domain.Enums;

namespace Cdns.Application.Interfaces;

/// <summary>
/// Ambient accessor for the authenticated member on the current request.
/// Resolved from the session-cookie claims by the Api layer and registered scoped.
/// Every member accessed throws if the request is unauthenticated, so callers in
/// authorized endpoints can treat the values as always present.
/// </summary>
public interface ICurrentUser
{
    /// <summary>The authenticated member's id (matches <c>Member.MemberId</c>, a bigint).</summary>
    long MemberId { get; }

    /// <summary>The authenticated member's login username (the <c>Name</c> claim).</summary>
    string UserName { get; }

    /// <summary>The authenticated member's type (the <c>MemberType</c> claim).</summary>
    MemberType MemberType { get; }

    /// <summary>True when a valid authenticated member is present on the request.</summary>
    bool IsAuthenticated { get; }
}
