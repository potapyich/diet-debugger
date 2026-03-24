using DietDebugger.Application.Auth;
using DietDebugger.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DietDebugger.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<DailySummaryService>();
        services.AddSingleton<IPatternBreakerAgent, PatternBreakerAgent>();
        services.AddScoped<PatternBreakerService>();
        services.AddScoped<ForecastService>();
        services.AddScoped<MonthlyReportService>();
        return services;
    }
}
