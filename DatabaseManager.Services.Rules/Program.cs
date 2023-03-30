using AutoMapper;
using DatabaseManager.Services.Rules;
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
        IMapper mapper = MappingConfig.RegisterMaps().CreateMapper();
        services.AddHttpClient<IDataSourceService, DataSourceService>();
        services.AddScoped<IDataSourceService, DataSourceService>();
        services.AddScoped<IDatabaseAccess, SQLServerAccess>();
        services.AddScoped<IRuleDBAccess, RuleDBAccess>();
        services.AddScoped<IFunctionAccess, FunctionAccess>();
        services.AddScoped<IPredictionSetAccess, PredictionSetAccess>();
        services.AddScoped<ITableStorageAccess, AzureTableStorageAccess>();
        services.AddScoped<IFileStorageAccess, AzureFileStorageAccess>();
        services.AddSingleton(mapper);
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
    })
    .Build();

host.Run();
