using DatabaseManager.Services.DataOpsManagement.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddScoped<ITableAccess, AzureTableAccess>();
        services.AddScoped<IFileStorage, AzureFileStorage>();
    })
    .Build();

host.Run();
