using AutoMapper;
using DatabaseManager.Services.RulesSqlite;
using DatabaseManager.Services.RulesSqlite.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        IMapper mapper = MappingConfig.RegisterMaps().CreateMapper();
        services.AddScoped<IRuleAccess, RuleAccess>();
        services.AddScoped<IFunctionAccess, FunctionAccess>();
        services.AddScoped<IDataAccess, SqliteDataAccess>();
        services.AddSingleton(mapper);
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
    })
    .Build();

host.Run();
