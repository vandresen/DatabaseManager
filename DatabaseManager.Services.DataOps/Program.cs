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

        // Just register IHttpClientFactory once — AddHttpClient() does this
        services.AddHttpClient();

        // Register services as scoped — they resolve IHttpClientFactory themselves
        services.AddScoped<IRuleAccess, RuleAccess>();
        services.AddScoped<IDataQc, DataQc>();
        services.AddScoped<IDataTransferAccess, DataTransferAccess>();
        services.AddScoped<IIndexAccess, IndexAccess>();
        services.AddScoped<IPredictionService, PredictionService>();
    })
    .Build();


host.Run();
