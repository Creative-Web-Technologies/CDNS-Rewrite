using Cdns.Infrastructure;
using Cdns.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

var host = builder.Build();
host.Run();
    