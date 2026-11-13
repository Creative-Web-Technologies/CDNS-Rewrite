using Cdns.Application.Auth;
using Cdns.Application.Interfaces;
using Cdns.Infrastructure.ExternalServices;
using Cdns.Infrastructure.Notifications;
using Cdns.Infrastructure.Persistence;
using Cdns.Infrastructure.Security;
using Cdns.Infrastructure.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Cdns.Application.Features.Signup;
using Cdns.Infrastructure.Services;

namespace Cdns.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Default is not configured.");

        services.AddDbContext<CdnsDbContext>(options =>
            options.UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<ISignupService, SignupService>();

        var useFake = environment.IsDevelopment() || configuration.GetValue<bool>("Ereg:UseFake");
        if (useFake)
            services.AddScoped<IEregService, FakeEregService>();
        else
            services.AddScoped<IEregService, ClosedEregService>();
        services.AddScoped<IEmailService, LoggingEmailService>();
        services.AddScoped<IAuthResultIssuer, MemberLoginSessionIssuer>();
        services.AddScoped<ILoginService, LoginService>();
        services.AddScoped<IMasterActivationService, MasterActivationService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();

        return services;
    }
}
