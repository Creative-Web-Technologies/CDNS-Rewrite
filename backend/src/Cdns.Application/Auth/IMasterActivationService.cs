namespace Cdns.Application.Auth;

public record CompleteProfileRequest(
    string FirstName,
    string LastName,
    string? Mobile,
    string? Nic,
    string? Designation);

public abstract record CompleteProfileResult
{
    public sealed record Success : CompleteProfileResult;
    public sealed record InvalidToken : CompleteProfileResult;
}

public abstract record SetActivationPasswordResult
{
    public sealed record Success(string SessionToken) : SetActivationPasswordResult;
    public sealed record InvalidToken : SetActivationPasswordResult;
}

public interface IMasterActivationService
{
    Task<CompleteProfileResult> CompleteProfileAsync(
        Guid activationToken,
        CompleteProfileRequest request,
        CancellationToken ct = default);

    Task<SetActivationPasswordResult> SetPasswordAsync(
        Guid activationToken,
        string newPassword,
        CancellationToken ct = default);
}
