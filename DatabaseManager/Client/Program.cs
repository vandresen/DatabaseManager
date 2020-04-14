using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using DatabaseManager.Client.Helpers;

namespace DatabaseManager.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");
            ConfigureServices(builder.Services);

            await builder.Build().RunAsync();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddBaseAddressHttpClient();
            services.AddOptions();
            services.AddSingleton<SingletonServices>();
            services.AddScoped<IHttpService, HttpService>();
            services.AddScoped<IDatabaseTransfer, DatabaseTransfer>();
            services.AddScoped<IDataSources, DataSources>();
            services.AddScoped<IDataModelCreate, DataModelCreate>();
            services.AddScoped<ICreateIndex, CreateIndex>();
            services.AddScoped<IIndexData, IndexData>();
        }
    }
}
