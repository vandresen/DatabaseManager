using DatabaseManager.AppFunctions.Services;
using DatabaseManager.Common.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(DatabaseManager.AppFunctions.Startup))]

namespace DatabaseManager.AppFunctions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();
            builder.Services.AddScoped<IHttpService, HttpService>();
        }
    }
}
