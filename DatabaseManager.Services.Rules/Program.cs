using DatabaseManager.Services.Rules.Services;
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
        services.AddScoped<IDatabaseAccess, DapperDataAccess>();
        services.AddScoped<IRuleDBAccess, RuleDBAccess>();
    })
    .Build();

host.Run();
