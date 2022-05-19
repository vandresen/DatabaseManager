using BlazorTable;
//using DatabaseManager.Client.Helpers;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using DatabaseManager.Common.Services;

namespace DatabaseManager.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");
            builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress), Timeout = TimeSpan.FromMinutes(5) });
            ConfigureServices(builder.Services);

            await builder.Build().RunAsync();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddSingleton<SingletonServices>();
            services.AddScoped<IHttpService, HttpService>();
            services.AddScoped<IDataSources, DataSourcesClient>();
            services.AddScoped<IDataModelCreate, DataModelCreate>();
            services.AddScoped<IDataIndexer, DataIndexer>();
            services.AddScoped<IIndexView, IndexView>();
            services.AddScoped<IRules, Rules>();
            services.AddScoped<IDataOps, DataOpsClientService>();
            services.AddScoped<IDataQc, DataQc>();
            services.AddScoped<ICookies, Cookies>();
            services.AddScoped<IDataTransfer, DataTransferClient>();
            services.AddScoped<IReportEdit, ReportEdit>();
            services.AddScoped<IDisplayMessage, DisplayMessage>();
            services.AddBlazorTable();
            services.AddMudServices();

        }
    }
}
