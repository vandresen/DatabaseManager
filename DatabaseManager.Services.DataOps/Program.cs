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
        services.AddHttpClient<IRuleAccess, RuleAccess>();
        services.AddScoped<IRuleAccess, RuleAccess>();
        services.AddHttpClient<IDataQc, DataQc>();
        services.AddScoped<IDataQc, DataQc>();
        services.AddHttpClient<IDataTransferAccess, DataTransferAccess>();
        services.AddScoped<IDataTransferAccess, DataTransferAccess>();
        services.AddHttpClient<IIndexAccess, IndexAccess>();
        services.AddScoped<IIndexAccess, IndexAccess>();
    })
    .Build();

host.Run();
