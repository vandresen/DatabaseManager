using DatabaseManager.Services.IndexSqlite;
using DatabaseManager.Services.IndexSqlite.Services;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IDataAccess, SqliteDataAccess>();
builder.Services.AddSingleton<IFileStorageService, AzureFileStorageService>();
builder.Services.AddHttpClient<IDataSourceService, DataSourceService>();
builder.Services.AddSingleton<IDataSourceService, DataSourceService>();
builder.Services.AddSingleton<IIndexAccess, IndexAccess>();
SD.DataSourceAPIBase = builder.Configuration["ServiceUrls:DataSourceAPI"];
SD.DataSourceKey = builder.Configuration["ServiceUrls:DataSourceKey"];

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.ConfigureApi();

app.Run();
