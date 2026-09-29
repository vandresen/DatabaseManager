using DatabaseManager.Services.Predictions;
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

SD.IndexSqliteAPI = builder.Configuration["IndexSqliteAPI"]
    ?? throw new InvalidOperationException("Setting 'IndexSqliteAPI' is missing.");
SD.IndexSqlServerAPI = builder.Configuration["IndexSqlServerAPI"]
    ?? throw new InvalidOperationException("Setting 'IndexSqlServerAPI' is missing.");
SD.IndexKeySetting = builder.Configuration["IndexKey"];

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
