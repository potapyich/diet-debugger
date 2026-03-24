using DietDebugger.Application.Interfaces;
using DietDebugger.Infrastructure.AI;
using DietDebugger.Infrastructure.Auth;
using DietDebugger.Infrastructure.Persistence;
using DietDebugger.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DietDebugger.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["DB_CONNECTION_STRING"]
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();

        var aiProvider = configuration["AI_MODEL_PROVIDER"] ?? "openai";
        var aiApiKey = configuration["AI_API_KEY"] ?? string.Empty;

        services.AddSingleton<IFoodVisionAgent>(sp =>
            new FoodVisionAgent(aiProvider, aiApiKey, sp.GetRequiredService<ILogger<FoodVisionAgent>>()));

        services.AddSingleton<IDailySummaryAgent>(sp =>
            new DailySummaryAgent(aiProvider, aiApiKey, sp.GetRequiredService<ILogger<DailySummaryAgent>>()));

        services.AddSingleton<IWeeklyPatternAgent>(sp =>
            new WeeklyPatternAgent(aiProvider, aiApiKey, sp.GetRequiredService<ILogger<WeeklyPatternAgent>>()));

        services.AddSingleton<IMonthlyStrategyAgent>(sp =>
            new MonthlyStrategyAgent(aiProvider, aiApiKey, sp.GetRequiredService<ILogger<MonthlyStrategyAgent>>()));

        var storagePath = configuration["FILE_STORAGE_PATH"] ?? Path.Combine(Path.GetTempPath(), "dietdebugger-files");
        services.AddSingleton(sp =>
            new LocalFileStorage(storagePath, sp.GetRequiredService<ILogger<LocalFileStorage>>()));
        services.AddSingleton<IFileStorage>(sp => sp.GetRequiredService<LocalFileStorage>());
        services.AddHostedService<FileCleanupService>();

        services.AddSingleton<AnalysisJobChannel>();
        services.AddHostedService<AnalysisJobProcessor>();

        return services;
    }
}
