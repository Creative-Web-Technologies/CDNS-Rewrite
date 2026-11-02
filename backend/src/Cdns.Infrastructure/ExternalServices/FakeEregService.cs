using Cdns.Application.Interfaces;

namespace Cdns.Infrastructure.ExternalServices;

// Real implementation (HMAC-signed HTTP to eReg) is explicitly deferred.
public class FakeEregService : IEregService
{
    public Task<bool> ValidateMasterActivation(string username, string password)
        => Task.FromResult(true);
}
