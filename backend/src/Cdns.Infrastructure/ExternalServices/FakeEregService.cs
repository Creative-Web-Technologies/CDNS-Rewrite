using Cdns.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cdns.Infrastructure.ExternalServices;

// Real implementation (HMAC-signed HTTP to eReg) is explicitly deferred.
public class FakeEregService : IEregService
{
    private readonly IConfiguration _config;

    public FakeEregService(IConfiguration config) => _config = config;

    public Task<bool> ValidateMasterActivation(string username, string password)
    {
        var validPassword = _config["Ereg:FakeValidPassword"] ?? "Ereg@12345";
        return Task.FromResult(password == validPassword);
    }
}

// Registered in non-Development environments when Ereg:UseFake is not set.
// Fail-closed: always returns false and logs an error so misconfiguration is visible.
internal sealed class ClosedEregService : IEregService
{
    private readonly ILogger<ClosedEregService> _logger;

    public ClosedEregService(ILogger<ClosedEregService> logger) => _logger = logger;

    public Task<bool> ValidateMasterActivation(string username, string password)
    {
        _logger.LogError(
            "eReg validation attempted but no real IEregService is registered. " +
            "Configure Ereg settings or set Ereg:UseFake=true.");
        return Task.FromResult(false);
    }
}
