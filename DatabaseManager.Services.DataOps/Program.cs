using DatabaseManager.Services.DataOps.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddHttpClient("DataOps", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(10);
        });

        services.AddScoped<IRuleAccess, RuleAccess>();
        services.AddScoped<IDataQc, DataQc>();
        services.AddScoped<IDataTransferAccess, DataTransferAccess>();
        services.AddScoped<IIndexAccess, IndexAccess>();
        services.AddScoped<IPredictionService, PredictionService>();
    })
    .Build();

host.Run();