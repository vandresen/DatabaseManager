using DatabaseManager.Services.DataConfiguration.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddScoped<IDataRepository, AzureFileRepository>();
    })
    .Build();

host.Run();
