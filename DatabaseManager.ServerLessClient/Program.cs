using Blazored.LocalStorage;
using BlazorTable;
using DatabaseManager.BlazorComponents;
using DatabaseManager.BlazorComponents.Services;
using DatabaseManager.ServerLessClient.Services;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using System;
using System.Net.Http;
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

            SD.DataSourceAPIBase = builder.Configuration["ServiceUrls:DataSourceAPI"];
            SD.DataSourceKey = builder.Configuration["ServiceUrls:DataSourceKey"];
            SD.DataConfigurationAPIBase = builder.Configuration["ServiceUrls:DataConfigurationAPI"];
            SD.DataConfigurationKey = builder.Configuration["ServiceUrls:DataConfigurationKey"];

            await builder.Build().RunAsync();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<SingletonServices>();
            services.AddBlazoredLocalStorage();

            services.AddHttpClient<IDataSourceService, DataSourceService>();

            services.AddScoped<IDataSourceService, DataSourceService>();

            services.AddScoped<IDisplayMessage, DisplayMessage>();
            services.AddScoped<IHttpService, HttpService>();
            services.AddScoped<IDataOps, DataOpsServerLess>();
            services.AddScoped<IDataSources, DataSourcesServerLess>();
            services.AddScoped<IDataTransfer, DataTransferServerLess>();
            services.AddScoped<IDataModelCreate, DataModelCreate>();
            services.AddScoped<IDataIndexer, DataIndexer>();
            services.AddScoped<IDataQc, DataQc>();
            services.AddScoped<IRules, Rules>();
            services.AddScoped<IReportEdit, ReportEdit>();
            services.AddScoped<IIndexView, IndexView>();
            services.AddScoped<ICookies, Cookies>();
            services.AddScoped<IDataConfiguration, DataConfiguration>();
            services.AddBlazorTable();
            services.AddMudServices();
        }
    }
}
