using System.Text.Json.Serialization;
using AssetFlow.Application.Services;
using AssetFlow.Domain.Repositories;
using AssetFlow.Infrastructure;
using AssetFlow.Infrastructure.Persistence;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using AssetFlow.API.Middleware;

LoadEnvironmentFile();

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddAssetFlowInfrastructure(builder.Configuration);

var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
var environmentOrigins = builder.Configuration["CORS_ALLOWED_ORIGINS"]?
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
var allowedOrigins = configuredOrigins
    .Concat(environmentOrigins)
    .Append("http://localhost:4200")
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("Frontend");
app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

app.Run();

static void LoadEnvironmentFile()
{
    for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
    {
        var candidates = new[]
        {
            Path.Combine(directory.FullName, ".env"),
            Path.Combine(directory.FullName, "backend", ".env")
        };
        var envFile = candidates.FirstOrDefault(File.Exists);
        if (envFile is not null)
        {
            Env.Load(envFile);
            return;
        }
    }
}

public partial class Program
{
}
