using DietDebugger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DietDebugger.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Meal> Meals { get; }
    DbSet<AnalysisJob> AnalysisJobs { get; }
    DbSet<DailySummary> DailySummaries { get; }
    DbSet<UserGoal> UserGoals { get; }
    DbSet<WeeklyReport> WeeklyReports { get; }
    DbSet<PatternEvent> PatternEvents { get; }
    DbSet<UserHabit> UserHabits { get; }
    DbSet<MonthlyReport> MonthlyReports { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
