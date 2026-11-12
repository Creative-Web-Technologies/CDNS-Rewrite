using Cdns.Api.Authentication;
using Cdns.Api.Endpoints.Auth;
using Cdns.Infrastructure;
using Cdns.Infrastructure.Persistence;
using Cdns.Infrastructure.Persistence.Seed;
using Cdns.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVite", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddAuthentication(SessionCookieAuthHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, SessionCookieAuthHandler>(
        SessionCookieAuthHandler.SchemeName, _ => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("MemberOnly", p => p.RequireClaim("MemberType", "Member"));
    options.AddPolicy("MasterOnly", p => p.RequireClaim("MemberType", "CompanyMasterMember"));
});
builder.Services.AddControllers();

var app = builder.Build();

// ForwardedHeaders — must run before all middleware that reads IsHttps / RemoteIpAddress.
// Development: keep loopback defaults only (safe locally).
// Other environments: replace defaults with explicitly configured IPs / CIDRs from appsettings:
//   Proxy:KnownProxies  — array of IP address strings
//   Proxy:KnownNetworks — array of CIDR strings (e.g. "10.0.0.0/8")
var fwdOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};

if (!app.Environment.IsDevelopment())
{
    fwdOptions.KnownProxies.Clear();
    fwdOptions.KnownIPNetworks.Clear();

    foreach (var proxy in app.Configuration.GetSection("Proxy:KnownProxies").Get<string[]>() ?? [])
        if (System.Net.IPAddress.TryParse(proxy, out var ip))
            fwdOptions.KnownProxies.Add(ip);

    foreach (var cidr in app.Configuration.GetSection("Proxy:KnownNetworks").Get<string[]>() ?? [])
        if (System.Net.IPNetwork.TryParse(cidr, out var network))
            fwdOptions.KnownIPNetworks.Add(network);
}

app.UseForwardedHeaders(fwdOptions);

app.UseCors("AllowVite");

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CdnsDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await AuthSeedData.SeedAsync(db, hasher);

    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapHealthChecks("/health");
app.MapAuthEndpoints();

app.Run();
