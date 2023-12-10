using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DatabaseManager.Services.DataQC.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddHttpClient<IRuleAccess, RuleAccess>();
        services.AddScoped<IRuleAccess, RuleAccess>();
        services.AddHttpClient<IIndexAccess, IndexAccess>();
        services.AddScoped<IIndexAccess, IndexAccess>();
        services.AddScoped<IDataQc, DataQcCore>();
        services.AddHttpClient<IConfigFileService, ConfigFileService>();
        services.AddScoped<IConfigFileService, ConfigFileService>();
        services.AddHttpClient<IDataSourceService, DataSourceService>();
        services.AddScoped<IDataSourceService, DataSourceService>();
    })
    .Build();

host.Run();
