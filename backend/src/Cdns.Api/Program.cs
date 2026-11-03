using Cdns.Api.Endpoints.Auth;
using Cdns.Infrastructure;
using Cdns.Infrastructure.Persistence;
using Cdns.Infrastructure.Persistence.Seed;
using Cdns.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddInfrastructure(builder.Configuration);

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

builder.Services.AddControllers();

var app = builder.Build();

app.UseCors("AllowVite"); 

app.UseRouting(); 

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
