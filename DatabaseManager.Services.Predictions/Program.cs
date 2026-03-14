using DatabaseManager.Services.Predictions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Register IHttpClientFactory
builder.Services.AddHttpClient();

// Register your services
builder.Services.AddScoped<IRuleAccess, RuleAccess>();
builder.Services.AddScoped<IIndexAccess, IndexAccess>();
builder.Services.AddScoped<IPrediction, PredictionCore>();
builder.Services.AddScoped<IDatabaseAccess, DapperDataAccess>();
builder.Services.AddScoped<IDatabaseManagementService, DatabaseManagementService>();
//builder.Services.AddScoped<IConfigFileService, ConfigFileService>();
//builder.Services.AddScoped<IDataQc, DataQcCore>();
//builder.Services.AddScoped<IDataSourceService, DataSourceService>();
//builder.Services.AddScoped<DataQcExecutionContext>();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
