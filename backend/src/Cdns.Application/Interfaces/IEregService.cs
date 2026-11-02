namespace Cdns.Application.Interfaces;

public interface IEregService
{
    Task<bool> ValidateMasterActivation(string username, string password);
}
