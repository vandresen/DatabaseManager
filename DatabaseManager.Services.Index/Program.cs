using DatabaseManager.Services.Index;
using DatabaseManager.Services.Index.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration(c =>
    {
        c.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables();
    })
    .ConfigureServices(services =>
    {
        services.AddHttpClient<IDataSourceService, DataSourceService>();
        services.AddScoped<IDataSourceService, DataSourceService>();
        services.AddScoped<IDapperDataAccess, DapperDataAccess>();
        services.AddScoped<IIndexDBAccess, IndexDBAccess>();
    })
    .Build();

host.Run();
