using DatabaseManager.Services.DataQuality.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

// Register IHttpClientFactory
builder.Services.AddHttpClient();

// Register your services
builder.Services.AddScoped<IRuleAccess, RuleAccess>();
builder.Services.AddScoped<IIndexAccess, IndexAccess>();
builder.Services.AddScoped<IConfigFileService, ConfigFileService>();
builder.Services.AddScoped<IDataQc, DataQcCore>();
builder.Services.AddScoped<IDataSourceService, DataSourceService>();

// Add Application Insights
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Build and run
builder.Build().Run();
