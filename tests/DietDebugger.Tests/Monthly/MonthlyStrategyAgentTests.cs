using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DietDebugger.Application;
using DietDebugger.Application.Interfaces;
using DietDebugger.Domain.Entities;
using DietDebugger.Infrastructure.AI;
using DietDebugger.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DietDebugger.Tests.Monthly;

[Trait("Category", "MonthlyStrategyAgent")]
public class MonthlyStrategyAgentTests : IClassFixture<MonthlyStrategyAgentTests.AppFactory>
{
    private readonly AppFactory _factory;
    public MonthlyStrategyAgentTests(AppFactory factory) => _factory = factory;

    public class AppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var toRemove = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    d.ServiceType == typeof(AppDbContext) ||
                    (d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>))
                ).ToList();
                foreach (var d in toRemove) services.Remove(d);
                services.AddEntityFrameworkInMemoryDatabase();
                services.AddDbContext<AppDbContext>((sp, opts) =>
                    opts.UseInMemoryDatabase("MonthlyTest").UseInternalServiceProvider(sp));

                var vision = services.SingleOrDefault(d => d.ServiceType == typeof(IFoodVisionAgent));
                if (vision != null) services.Remove(vision);
                services.AddSingleton<IFoodVisionAgent>(new StubVision());

                var monthlyAgent = services.SingleOrDefault(d => d.ServiceType == typeof(IMonthlyStrategyAgent));
                if (monthlyAgent != null) services.Remove(monthlyAgent);
                services.AddSingleton<IMonthlyStrategyAgent>(new StubMonthlyAgent());
            });
        }

        public class StubVision : IFoodVisionAgent
        {
            public Task<FoodVisionResult> AnalyzeAsync(FoodVisionInput input, CancellationToken ct = default) =>
                Task.FromResult(new FoodVisionResult([], 400, 30, 15, 50, "medium", 0.9));
        }

        public class StubMonthlyAgent : IMonthlyStrategyAgent
        {
            public Task<StrategyAssessment> GenerateAsync(MonthlyStrategyContext context, CancellationToken ct = default) =>
                Task.FromResult(new StrategyAssessment(
                    ["You are on track.", "Keep up the consistency."],
                    new StrategyForecast(-1.5m, 80m, null)));
        }
    }

    private async Task<(HttpClient client, Guid userId)> RegisterAsync()
    {
        var client = _factory.CreateClient();
        var email = $"monthly_{Guid.NewGuid()}@test.com";
        var resp = await client.PostAsync("/auth/register",
            new StringContent(JsonSerializer.Serialize(new { email, password = "Password1!" }),
                Encoding.UTF8, "application/json"));
        var body = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var token = body.RootElement.GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == email);
        return (client, user.Id);
    }

    private async Task SeedWeeklyReport(Guid userId, DateOnly weekStart)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.WeeklyReports.Add(new WeeklyReport
        {
            UserId = userId,
            WeekStartDate = weekStart,
            Patterns = JsonSerializer.Serialize(new[] { "Pattern 1", "Pattern 2" }),
            CalendarDayStatuses = "[]",
            GeneratedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task MonthlyReport_Returns404_BeforeWeeklyData()
    {
        var (client, _) = await RegisterAsync();

        var resp = await client.GetAsync("/monthly-report?month=2026-03");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    [Trait("Category", "MonthlyReport_Returns404_BeforeWeeklyData")]
    public async Task MonthlyReport_Returns404_NoData_Trait()
    {
        var (client, _) = await RegisterAsync();
        var resp = await client.GetAsync("/monthly-report?month=2025-01");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task MonthlyReport_GeneratesWhenWeeklyDataExists()
    {
        var (client, userId) = await RegisterAsync();
        var weekStart = new DateOnly(2026, 3, 2); // First Monday in March
        await SeedWeeklyReport(userId, weekStart);

        var resp = await client.GetAsync("/monthly-report?month=2026-03");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var body = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var assessment = body.RootElement.GetProperty("assessment");
        Assert.True(assessment.GetProperty("insights").GetArrayLength() > 0);
    }
}
