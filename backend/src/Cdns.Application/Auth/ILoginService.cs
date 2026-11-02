namespace Cdns.Application.Auth;

public abstract record LoginResult
{
    public sealed record Success(string SessionToken) : LoginResult;

    // eReg validated — client must proceed through profile-fill + set-password before a session is issued
    public sealed record MasterActivationRequired(Guid ActivationToken) : LoginResult;

    // Wrong password OR eReg call failed — intentionally indistinguishable to callers (matches legacy)
    public sealed record InvalidCredentials : LoginResult;

    public sealed record AccountLocked : LoginResult;
}

public interface ILoginService
{
    Task<LoginResult> LoginAsync(string userName, string password, CancellationToken ct = default);
}
