using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using DatabaseManager.Client.Helpers;
using System.Net.Http;
using BlazorTable;

namespace DatabaseManager.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");
            builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            ConfigureServices(builder.Services);

            await builder.Build().RunAsync();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddSingleton<SingletonServices>();
            services.AddScoped<IHttpService, HttpService>();
            services.AddScoped<IDataSources, DataSources>();
            services.AddScoped<IDataModelCreate, DataModelCreate>();
            services.AddScoped<ICreateIndex, CreateIndex>();
            services.AddScoped<IIndexData, IndexData>();
            services.AddScoped<IDataFile, DataFile>();
            services.AddScoped<IRules, Rules>();
            services.AddScoped<IFunctions, Functions>();
            services.AddScoped<IDataQc, DataQc>();
            services.AddScoped<IPrediction, Prediction>();
            services.AddScoped<ICookies, Cookies>();
            services.AddScoped<IDataTransfer, DataTransfer>();
            services.AddScoped<IDisplayMessage, DisplayMessage>();
            services.AddBlazorTable();
        }
    }
}
