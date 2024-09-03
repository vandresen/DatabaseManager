using AutoMapper;
using Blazored.LocalStorage;
using BlazorTable;
using DatabaseManager.BlazorComponents.Services;
using DatabaseManager.ServerLessClient.Models;
using dymaptic.GeoBlazor.Core;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

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

            using var response = await http.GetAsync("appsettings.json");
            using var stream = await response.Content.ReadAsStreamAsync();
            builder.Configuration.AddJsonStream(stream);
            var config = builder.Configuration.Build();
            bool sqlite = config.GetValue<bool>("Sqlite");
            if (!string.IsNullOrEmpty(config["ArcGISApiKey"]))
            {
                SD.EsriKey = builder.Configuration["ArcGISApiKey"];
            }

            var dataOpsApiUrl = config["ServiceUrls:DataOpsAPI"];
            var dataOpsKey = config["ServiceUrls:DataOpsKey"];

            builder.Services.AddHttpClient("DataOpsAPI", client =>
            {
                client.BaseAddress = new Uri(dataOpsApiUrl);
                client.DefaultRequestHeaders.Add("x-functions-key", dataOpsKey);
                client.Timeout = TimeSpan.FromMinutes(5);
            });

            ConfigureServices(builder.Services, sqlite);

            SD.Sqlite = sqlite;
            SD.DataSourceAPIBase = builder.Configuration["ServiceUrls:DataSourceAPI"];
            SD.DataSourceKey = builder.Configuration["ServiceUrls:DataSourceKey"];
            SD.IndexAPIBase = builder.Configuration["ServiceUrls:IndexAPI"];
            SD.IndexKey = builder.Configuration["ServiceUrls:IndexKey"];
            SD.DataConfigurationAPIBase = builder.Configuration["ServiceUrls:DataConfigurationAPI"];
            SD.DataConfigurationKey = builder.Configuration["ServiceUrls:DataConfigurationKey"];
            SD.DataModelAPIBase = builder.Configuration["ServiceUrls:DataModelAPI"];
            SD.DataModelKey = builder.Configuration["ServiceUrls:DataModelKey"];
            SD.DataRuleAPIBase = builder.Configuration["ServiceUrls:DataRuleAPI"];
            SD.DataRuleKey = builder.Configuration["ServiceUrls:DataRuleKey"];
            SD.DataTransferAPIBase = builder.Configuration["ServiceUrls:DataTransferAPI"];
            SD.DataTransferKey = builder.Configuration["ServiceUrls:DataTransferKey"];
            SD.DataOpsManageAPIBase = builder.Configuration["ServiceUrls:DataOpsManageAPI"];
            SD.DataOpsManageKey = builder.Configuration["ServiceUrls:DataOpsManageKey"];

            await builder.Build().RunAsync();
        }

        private static void ConfigureServices(IServiceCollection services, bool sqlite)
        {
            IMapper mapper = MappingConfig.RegisterMaps().CreateMapper();
            services.AddSingleton(mapper);

            services.AddSingleton<BlazorSingletonService>();
            services.AddSingleton<DatabaseManager.Shared.SingletonServices>();
            services.AddBlazoredLocalStorage();

            services.AddHttpClient();
            services.AddHttpClient<DatabaseManager.ServerLessClient.Services.IDataSources, DatabaseManager.ServerLessClient.Services.DataSources>();
            services.AddHttpClient<IDataModelService, DataModelService>();
            services.AddHttpClient<IDataConfiguration, DataConfiguration>();
            services.AddHttpClient<IRulesService, RulesService>();
            
            services.AddHttpClient<IDataIndexer, DataIndexerServerLess>();

            services.AddScoped<DatabaseManager.ServerLessClient.Services.IDataSources, DatabaseManager.ServerLessClient.Services.DataSources>();
            services.AddScoped<IDataModelService, DataModelService>();
            services.AddScoped<IDisplayMessage, DisplayMessage>();
            services.AddScoped<IHttpService, HttpService>();
            services.AddScoped<DatabaseManager.ServerLessClient.Services.IDataOps, DatabaseManager.ServerLessClient.Services.DataOps>();
            //services.AddScoped<IDataSources, DataSourcesServerLess>();
            services.AddScoped<IDataTransfer, DataTransferServerLess>();
            services.AddScoped<IDataModelCreate, DataModelCreateServerless>();
            services.AddScoped<IDataIndexer, DataIndexerServerLess>();
            services.AddScoped<IDataQc, DataQc>();
            services.AddScoped<IRules, RulesServerless>();
            services.AddScoped<IRulesService, RulesService>();
            services.AddScoped<IReportEdit, ReportEdit>();
            services.AddScoped<ISync, Sync>();

            services.AddScoped<ICookies, Cookies>();
            services.AddScoped<IDataConfiguration, DataConfiguration>();

            if (sqlite)
            {
                services.AddHttpClient<IIndexView, IndexViewSqlite>();
                services.AddScoped<IIndexView, IndexViewSqlite>();
            }
            else
            {
                services.AddHttpClient<IIndexView, IndexViewServerless>();
                services.AddScoped<IIndexView, IndexViewServerless>();
            }

            services.AddBlazorTable();
            services.AddMudServices();
            services.AddGeoBlazor();
        }
    }
}
