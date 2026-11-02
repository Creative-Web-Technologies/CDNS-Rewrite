namespace Cdns.Application.Interfaces;

public interface IAuthResultIssuer
{
    Task<string> IssueAsync(long memberId, string userName, CancellationToken ct = default);
}
