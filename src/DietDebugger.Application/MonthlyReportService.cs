using System.Collections.Concurrent;
using System.Text.Json;
using DietDebugger.Application.Interfaces;
using DietDebugger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DietDebugger.Application;

public class MonthlyReportService(IAppDbContext db, IMonthlyStrategyAgent agent, ForecastService forecastService)
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public async Task<MonthlyReport?> GetOrGenerateAsync(
        Guid userId,
        DateOnly monthStart,
        string? userLanguage,
        CancellationToken ct = default)
    {
        if (monthStart.Day != 1)
            monthStart = new DateOnly(monthStart.Year, monthStart.Month, 1);

        // Require weekly data before generating
        var weeklyReports = await db.WeeklyReports
            .Where(w => w.UserId == userId
                     && w.WeekStartDate >= monthStart
                     && w.WeekStartDate < monthStart.AddMonths(1))
            .OrderBy(w => w.WeekStartDate)
            .ToListAsync(ct);

        if (weeklyReports.Count == 0)
            return null;

        var key = $"{userId}:{monthStart}";
        var sem = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        await sem.WaitAsync(ct);
        try
        {
            var existing = await db.MonthlyReports
                .FirstOrDefaultAsync(r => r.UserId == userId && r.MonthStart == monthStart, ct);

            if (existing != null)
                return existing;

            var profile = await db.Users.FindAsync([userId], ct);
            var goal = await db.UserGoals.FirstOrDefaultAsync(g => g.UserId == userId, ct);
            var forecast = await forecastService.GetForecastAsync(userId, ct);

            var ctx = new MonthlyStrategyContext(weeklyReports, profile, goal, forecast, userLanguage);
            var assessment = await agent.GenerateAsync(ctx, ct);

            var report = new MonthlyReport
            {
                UserId = userId,
                MonthStart = monthStart,
                Assessment = JsonSerializer.Serialize(assessment),
                GeneratedAt = DateTime.UtcNow
            };

            db.MonthlyReports.Add(report);
            await db.SaveChangesAsync(ct);
            return report;
        }
        finally
        {
            sem.Release();
        }
    }
}
