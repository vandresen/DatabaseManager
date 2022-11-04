using AutoMapper;
using DatabaseManager.Services.Datasources.Repository;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Xml.Linq;

[assembly: FunctionsStartup(typeof(DatabaseManager.Services.Datasources.Startup))]

namespace DatabaseManager.Services.Datasources
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            IMapper mapper = MappingConfig.RegisterMaps().CreateMapper();
            builder.Services.AddSingleton(mapper);
            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            builder.Services.AddScoped<IDataSourceRepository, DataSourceRepository>();
            //builder.Services.AddHttpClient();

            //builder.Services.AddSingleton<IMyService>((s) => {
            //    return new MyService();
            //});

            //builder.Services.AddSingleton<ILoggerProvider, MyLoggerProvider>();
        }
    }
}
