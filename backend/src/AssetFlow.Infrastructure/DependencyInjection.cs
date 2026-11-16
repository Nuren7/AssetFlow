using AssetFlow.Application.Services;
using AssetFlow.Application.Tools;
using AssetFlow.Domain.Repositories;
using AssetFlow.Infrastructure.AI;
using AssetFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssetFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAssetFlowInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var databaseUrl = configuration.GetConnectionString("Default") ?? configuration["DATABASE_URL"];
        if (string.IsNullOrWhiteSpace(databaseUrl))
        {
            throw new InvalidOperationException("Set the DATABASE_URL environment variable or ConnectionStrings:Default configuration.");
        }

        services.AddDbContext<AssetFlowDbContext>(options =>
            options.UseNpgsql(DatabaseUrlParser.ToConnectionString(databaseUrl)));
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<DatabaseSeeder>();

        services.AddSingleton<IGroqChatClient>(_ => new GroqChatClient(
            configuration["GROQ_API_KEY"],
            configuration["GROQ_MODEL"] ?? "llama-3.3-70b-versatile"));
        services.AddScoped<IAiAssistantService, GroqAiAssistantService>();
        services.AddScoped<IAssetToolExecutor, AssetToolExecutor>();
        return services;
    }
}
