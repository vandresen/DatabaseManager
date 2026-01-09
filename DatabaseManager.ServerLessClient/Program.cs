using Blazored.LocalStorage;
using BlazorTable;
using DatabaseManager.ServerLessClient.Models;
using DatabaseManager.ServerLessClient.Services;
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

            builder.Services.AddHttpClient("DataOpsAPI", client =>
            {
                client.Timeout = TimeSpan.FromMinutes(5);
            });

            builder.Services.AddGeoBlazor(builder.Configuration);
            ConfigureServices(builder.Services, sqlite);

            SD.Sqlite = sqlite;
            SD.DataSourceAPIBase = builder.Configuration["ServiceUrls:DataSourceAPI"];
            SD.DataSourceKey = builder.Configuration["ServiceUrls:DataSourceKey"];
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
            services.AddSingleton<BlazorSingletonService>();
            services.AddBlazoredLocalStorage();

            services.AddHttpClient();
            services.AddHttpClient<IDataSources, DataSources>();
            services.AddHttpClient<IRuleService, RuleService>();
            services.AddHttpClient<IReport, ReportService>();

            //services.AddHttpClient<IDataIndexer, DataIndexerServerLess>();

            services.AddScoped<IDataSources, DataSources>();
            //services.AddScoped<IDataModelService, DataModelService>();
            //services.AddScoped<IDisplayMessage, DisplayMessage>();
            services.AddScoped<IPopupMessage, PopupMessage>();
            //services.AddScoped<IHttpService, HttpService>();
            services.AddScoped<IDataOps, DataOps>();
            //services.AddScoped<IDataTransfer, DataTransferServerLess>();
            //services.AddScoped<IDataModelCreate, DataModelCreateServerless>();
            //services.AddScoped<IDataIndexer, DataIndexerServerLess>();
            services.AddScoped<IReport, ReportService>();
            services.AddScoped<IRuleService, RuleService>();
            //services.AddScoped<IReportEdit, ReportEdit>();
            //services.AddScoped<ISync, Sync>();
            //services.AddScoped<ICookies, Cookies>();
            services.AddScoped<Services.IDataConfigurationService, Services.DataConfigurationService>();

            if (sqlite)
            {
                services.AddScoped<Services.IIndexView, IndexViewSqlLite>();
            }
            else
            {
                services.AddScoped<Services.IIndexView, IndexViewSqlServer>();
            }

            services.AddBlazorTable();
            services.AddMudServices();
        }
    }
}
