using DatabaseManager.Services.IndexSqlite;
using DatabaseManager.Services.IndexSqlite.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

//This is for local testing, the url may have to be changed
builder.Services.AddCors(options =>
{
    options.AddPolicy("MyCorsPolicy", builder =>
    {
        builder.WithOrigins("https://localhost:44343") // Replace with the correct origin of your Blazor app.
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
    app.UseCors("MyCorsPolicy");
}  

app.UseHttpsRedirection();

app.ConfigureApi();

app.Run();
