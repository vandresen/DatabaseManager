using AutoMapper;
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

            var http = new HttpClient()
            {
                BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
                Timeout = TimeSpan.FromMinutes(5)
            };
            builder.Services.AddScoped(sp => http);

            ConfigureServices(builder.Services);

            using var response = await http.GetAsync("appsettings.json");
            using var stream = await response.Content.ReadAsStreamAsync();
            builder.Configuration.AddJsonStream(stream);

            SD.DataSourceAPIBase = builder.Configuration["ServiceUrls:DataSourceAPI"];
            SD.DataSourceKey = builder.Configuration["ServiceUrls:DataSourceKey"];
            SD.IndexAPIBase = builder.Configuration["ServiceUrls:IndexAPI"];
            SD.IndexKey = builder.Configuration["ServiceUrls:IndexKey"];
            SD.DataConfigurationAPIBase = builder.Configuration["ServiceUrls:DataConfigurationAPI"];
            SD.DataConfigurationKey = builder.Configuration["ServiceUrls:DataConfigurationKey"];
            SD.DataModelAPIBase = builder.Configuration["ServiceUrls:DataModelAPI"];
            SD.DataModelKey = builder.Configuration["ServiceUrls:DataModelKey"];

            await builder.Build().RunAsync();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            IMapper mapper = MappingConfig.RegisterMaps().CreateMapper();
            services.AddSingleton(mapper);

            services.AddSingleton<SingletonServices>();
            services.AddBlazoredLocalStorage();

            services.AddHttpClient<IDataSourceService, DataSourceService>();
            services.AddHttpClient<IIndexService, IndexService>();
            services.AddHttpClient<IDataModelService, DataModelService>();

            services.AddScoped<IDataSourceService, DataSourceService>();
            services.AddScoped<IIndexService, IndexService>();
            services.AddScoped<IDataModelService, DataModelService>();

            services.AddScoped<IDisplayMessage, DisplayMessage>();
            services.AddScoped<IHttpService, HttpService>();
            services.AddScoped<IDataOps, DataOpsServerLess>();
            services.AddScoped<IDataSources, DataSourcesServerLess>();
            services.AddScoped<IDataTransfer, DataTransferServerLess>();
            services.AddScoped<IDataModelCreate, DataModelCreateServerless>();
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
