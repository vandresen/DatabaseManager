using Blazored.LocalStorage;
using BlazorTable;
using DatabaseManager.Common.Services;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.ServerLessClient
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress), Timeout = TimeSpan.FromMinutes(5) });

            ConfigureServices(builder.Services);

            await builder.Build().RunAsync();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<SingletonService>();
            services.AddBlazoredLocalStorage();
            services.AddScoped<IDisplayMessage, DisplayMessage>();
            services.AddScoped<IHttpService, HttpService>();
            services.AddScoped<IDataOps, DataOpsServerLess>();
            services.AddScoped<IDataSources, DataSourcesServerLess>();
            services.AddScoped<IDataTransfer, DataTransferServerLess>();
            services.AddScoped<IDataModelCreate, DataModelCreate>();
            services.AddScoped<IDataIndexer, DataIndexer>();
            services.AddScoped<IDataQc, DataQc>();
            services.AddScoped<IRules, Rules>();
            services.AddScoped<IIndexView, IndexView>();
            services.AddBlazorTable();
            services.AddMudServices();
        }
    }
}
