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

            builder.Services.AddBaseAddressHttpClient();
            builder.Services.AddSingleton<SingletonServices>();
            builder.Services.AddScoped<IHttpService, HttpService>();
            builder.Services.AddScoped<IDatabaseTransfer, DatabaseTransfer>();
            builder.Services.AddScoped<IDataSources, DataSources>();
            builder.Services.AddScoped<IDataModelCreate, DataModelCreate>();

            await builder.Build().RunAsync();
        }
    }
}
