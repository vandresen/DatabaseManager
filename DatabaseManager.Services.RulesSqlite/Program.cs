using AutoMapper;
using DatabaseManager.Services.RulesSqlite;
using DatabaseManager.Services.RulesSqlite.Services;
using Microsoft.Extensions.Logging.AzureAppServices;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddAzureWebAppDiagnostics();
builder.Services.Configure<AzureBlobLoggerOptions>(options =>
{
    options.BlobName = "log.txt";
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

IMapper mapper = MappingConfig.RegisterMaps().CreateMapper();
builder.Services.AddSingleton(mapper);
builder.Services.AddSingleton<IRuleAccess, RuleAccess>();
builder.Services.AddSingleton<IDataAccess, SqliteDataAccess>();
builder.Services.AddSingleton<IFunctionAccess, FunctionAccess>();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

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
