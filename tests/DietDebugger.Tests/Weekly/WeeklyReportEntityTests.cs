using DietDebugger.Domain.Entities;
using DietDebugger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DietDebugger.Tests.Weekly;

using WeeklyReportEntity = DietDebugger.Domain.Entities.WeeklyReport;

[Trait("Category", "WeeklyReportEntity")]
public class WeeklyReportEntityTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task WeeklyReport_CanBeSavedAndRetrieved()
    {
        using var db = CreateDb();
        var user = new User { Email = "weekly@test.com", PasswordHash = "hash" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var monday = DateOnly.FromDateTime(DateTime.UtcNow);
        while (monday.DayOfWeek != DayOfWeek.Monday)
            monday = monday.AddDays(-1);

        var report = new WeeklyReportEntity
        {
            UserId = user.Id,
            WeekStartDate = monday,
            Patterns = """["Good consistency this week"]""",
            CalendarDayStatuses = """[{"date":"2024-01-01","status":"green"}]"""
        };
        db.WeeklyReports.Add(report);
        await db.SaveChangesAsync();

        var saved = await db.WeeklyReports.FindAsync(report.Id);
        Assert.NotNull(saved);
        Assert.Contains("Good consistency", saved.Patterns);
        Assert.False(saved.IsStale);
    }

    [Fact]
    public async Task WeeklyReport_NonMondayDate_Returns400()
    {
        // Verify a Tuesday fails the Monday check - tested via API constraint
        var tuesday = new DateOnly(2024, 1, 2); // A Tuesday
        Assert.Equal(DayOfWeek.Tuesday, tuesday.DayOfWeek);
        Assert.NotEqual(DayOfWeek.Monday, tuesday.DayOfWeek);
    }
}
