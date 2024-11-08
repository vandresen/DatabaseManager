using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DatabaseManager.Services.Reports.Services;
using Microsoft.Extensions.Configuration;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration(c =>
    {
        c.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables();
    })
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddHttpClient<IRuleAccess, RuleAccess>();
        services.AddScoped<IRuleAccess, RuleAccess>();
        services.AddHttpClient<IIndexAccess, IndexAccess>();
        services.AddScoped<IIndexAccess, IndexAccess>();
    })
    .Build();

host.Run();
