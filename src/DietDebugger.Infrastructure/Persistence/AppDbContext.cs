using DietDebugger.Application.Interfaces;
using DietDebugger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DietDebugger.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Meal> Meals => Set<Meal>();
    public DbSet<AnalysisJob> AnalysisJobs => Set<AnalysisJob>();
    public DbSet<DailySummary> DailySummaries => Set<DailySummary>();
    public DbSet<UserGoal> UserGoals => Set<UserGoal>();
    public DbSet<WeeklyReport> WeeklyReports => Set<WeeklyReport>();
    public DbSet<PatternEvent> PatternEvents => Set<PatternEvent>();
    public DbSet<UserHabit> UserHabits => Set<UserHabit>();
    public DbSet<MonthlyReport> MonthlyReports => Set<MonthlyReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).IsRequired().HasMaxLength(256);
            e.Property(u => u.Sex).HasConversion<string?>();
            e.Property(u => u.DietType).HasConversion<string?>();
            e.Property(u => u.FeedbackTone).HasConversion<string>();
            e.Property(u => u.WeightKg).HasPrecision(5, 1);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasOne(t => t.User).WithMany(u => u.RefreshTokens).HasForeignKey(t => t.UserId);
        });

        modelBuilder.Entity<PatternEvent>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Type).HasConversion<string>();
            e.HasIndex(p => new { p.UserId, p.Acknowledged, p.TriggeredAt });
        });

        modelBuilder.Entity<UserHabit>(e =>
        {
            e.HasKey(h => h.Id);
            e.Property(h => h.HabitDescription).IsRequired().HasMaxLength(200);
            e.HasIndex(h => h.UserId);
        });

        modelBuilder.Entity<MonthlyReport>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasIndex(m => new { m.UserId, m.MonthStart }).IsUnique();
            e.Property(m => m.Assessment).HasColumnType("jsonb");
        });

        modelBuilder.Entity<WeeklyReport>(e =>
        {
            e.HasKey(w => w.Id);
            e.HasIndex(w => new { w.UserId, w.WeekStartDate }).IsUnique();
            e.Property(w => w.Patterns).HasColumnType("jsonb");
            e.Property(w => w.CalendarDayStatuses).HasColumnType("jsonb");
        });

        modelBuilder.Entity<UserGoal>(e =>
        {
            e.HasKey(g => g.Id);
            e.HasIndex(g => g.UserId).IsUnique();
            e.Property(g => g.GoalType).HasConversion<string>();
        });

        modelBuilder.Entity<DailySummary>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasIndex(d => new { d.UserId, d.Date }).IsUnique();
            e.Property(d => d.TotalCalories).HasPrecision(10, 2);
            e.Property(d => d.TotalProteinG).HasPrecision(10, 2);
            e.Property(d => d.TotalFatG).HasPrecision(10, 2);
            e.Property(d => d.TotalCarbsG).HasPrecision(10, 2);
            e.Property(d => d.Insights).HasColumnType("jsonb");
        });

        modelBuilder.Entity<AnalysisJob>(e =>
        {
            e.HasKey(j => j.Id);
            e.Property(j => j.Status).HasConversion<string>();
            e.HasIndex(j => new { j.UserId, j.CreatedAt });
        });

        modelBuilder.Entity<Meal>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Calories).HasPrecision(10, 2);
            e.Property(m => m.ProteinG).HasPrecision(10, 2);
            e.Property(m => m.FatG).HasPrecision(10, 2);
            e.Property(m => m.CarbsG).HasPrecision(10, 2);
            e.Property(m => m.ConfidenceScore).HasPrecision(5, 4);
            e.Property(m => m.Ingredients).HasColumnType("jsonb");
            e.Property(m => m.Source).HasConversion<string>();
            e.HasOne(m => m.User).WithMany().HasForeignKey(m => m.UserId);
            e.HasIndex(m => new { m.UserId, m.LoggedAt });
        });
    }
}
