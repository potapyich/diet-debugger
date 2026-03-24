using System.Net;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using DietDebugger.Application.Interfaces;
using DietDebugger.Infrastructure.AI;
using DietDebugger.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DietDebugger.Tests.Validation;

[Trait("Category", "Validation")]
public class ValidationTests : IClassFixture<ValidationTests.AppFactory>
{
    private readonly AppFactory _factory;
    public ValidationTests(AppFactory factory) => _factory = factory;

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
                    opts.UseInMemoryDatabase("ValidationTest").UseInternalServiceProvider(sp));
                var agentDesc = services.SingleOrDefault(d => d.ServiceType == typeof(IFoodVisionAgent));
                if (agentDesc != null) services.Remove(agentDesc);
                services.AddSingleton<IFoodVisionAgent>(new StubVision());
            });
        }

        public class StubVision : IFoodVisionAgent
        {
            public Task<FoodVisionResult> AnalyzeAsync(FoodVisionInput input, CancellationToken ct = default) =>
                Task.FromResult(new FoodVisionResult([], 400, 30, 15, 50, "medium", 0.9));
        }
    }

    private HttpClient CreateClient() => _factory.CreateClient();

    private static StringContent Json(object obj) =>
        new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");

    [Fact]
    [Trait("Category", "Validation_InvalidEmail_Returns400")]
    public async Task Validation_InvalidEmail_Returns400()
    {
        var client = CreateClient();
        var resp = await client.PostAsync("/auth/register",
            Json(new { email = "bad-email", password = "Password1!" }));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var body = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Validation_ShortPassword_Returns400()
    {
        var client = CreateClient();
        var resp = await client.PostAsync("/auth/register",
            Json(new { email = "test@test.com", password = "abc" }));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Validation_PasswordNoNumber_Returns400()
    {
        var client = CreateClient();
        var resp = await client.PostAsync("/auth/register",
            Json(new { email = "test@test.com", password = "NoNumbersHere" }));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    private async Task<HttpClient> RegisterAsync()
    {
        var client = CreateClient();
        var email = $"val_{Guid.NewGuid()}@test.com";
        var resp = await client.PostAsync("/auth/register",
            Json(new { email, password = "Password1!" }));
        var body = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var token = body.RootElement.GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    [Trait("Category", "Validation_WeightOutOfRange_Returns400")]
    public async Task Validation_WeightOutOfRange_Returns400()
    {
        var client = await RegisterAsync();
        var resp = await client.PutAsync("/profile",
            Json(new { weightKg = 5 })); // below 30kg minimum
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var body = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Validation_GoalCaloriesOutOfRange_Returns400()
    {
        var client = await RegisterAsync();
        var resp = await client.PutAsync("/goals",
            Json(new { goalType = 0, dailyCalorieTarget = 100 })); // below 800
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Validation_ValidGoal_Returns200()
    {
        var client = await RegisterAsync();
        var resp = await client.PutAsync("/goals",
            Json(new { goalType = 0, dailyCalorieTarget = 1800 }));
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task Validation_EmptyHabit_Returns400()
    {
        var client = await RegisterAsync();
        var resp = await client.PostAsync("/habits",
            Json(new { habitDescription = "" }));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var body = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.TryGetProperty("errors", out _));
    }
}
