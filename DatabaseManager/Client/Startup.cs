using DatabaseManager.Client.Helpers;
using Microsoft.AspNetCore.Components.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace DatabaseManager.Client
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<SingletonServices>();
            services.AddScoped<IHttpService, HttpService>();
            services.AddScoped<IDatabaseTransfer, DatabaseTransfer>();
            services.AddScoped<IDataSources, DataSources>();
            services.AddScoped<IDataModelCreate, DataModelCreate>();
        }

        public void Configure(IComponentsApplicationBuilder app)
        {
            app.AddComponent<App>("app");
        }
    }
}
