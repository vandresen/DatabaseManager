using Blazored.LocalStorage;
using BlazorTable;
using DatabaseManager.BlazorComponents;
using DatabaseManager.BlazorComponents.Services;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
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

            var http = new HttpClient()
            {
                BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
            };

            builder.Services.AddScoped(sp => http);

            using var response = await http.GetAsync("appsettings.json");
            using var stream = await response.Content.ReadAsStreamAsync();

            builder.Configuration.AddJsonStream(stream);

            SD.DataSourceAPIBase = builder.Configuration["ServiceUrls:DataSourceAPI"];
            SD.DataSourceKey = builder.Configuration["ServiceUrls:DataSourceKey"];
            SD.IndexAPIBase = builder.Configuration["ServiceUrls:IndexAPI"];
            SD.IndexKey = builder.Configuration["ServiceUrls:IndexKey"];
            SD.DataConfigurationAPIBase = builder.Configuration["ServiceUrls:DataConfigurationAPI"];
            SD.DataConfigurationKey = builder.Configuration["ServiceUrls:DataConfigurationKey"];
        

            await builder.Build().RunAsync();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<SingletonServices>();
            services.AddBlazoredLocalStorage();

            services.AddHttpClient<IDataSourceService, DataSourceService>();
            services.AddHttpClient<IIndexService, IndexService>();

            services.AddScoped<IDataSourceService, DataSourceService>();
            services.AddScoped<IIndexService, IndexService>();

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
            services.AddScoped<IIndexView, IndexViewServerless>();
            services.AddScoped<ICookies, Cookies>();
            services.AddScoped<IDataConfiguration, DataConfiguration>();
            
            services.AddBlazorTable();
            services.AddMudServices();
        }
    }
}
