using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DietDebugger.Application.Interfaces;
using DietDebugger.Domain.Entities;
using DietDebugger.Infrastructure.AI;
using DietDebugger.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DietDebugger.Tests.Profile;

[Trait("Category", "GoalsApi")]
public class GoalsApiTests : IClassFixture<GoalsApiTests.AppFactory>
{
    private readonly AppFactory _factory;
    public GoalsApiTests(AppFactory factory) => _factory = factory;

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
                    opts.UseInMemoryDatabase("GoalsApiTest").UseInternalServiceProvider(sp));
                var agentDesc = services.SingleOrDefault(d => d.ServiceType == typeof(IFoodVisionAgent));
                if (agentDesc != null) services.Remove(agentDesc);
                services.AddSingleton<IFoodVisionAgent>(new StubFoodVision());
            });
        }
        public class StubFoodVision : IFoodVisionAgent
        {
            public Task<FoodVisionResult> AnalyzeAsync(FoodVisionInput input, CancellationToken ct = default) =>
                Task.FromResult(new FoodVisionResult([], 400, 30, 15, 50, "medium", 0.9));
        }
    }

    private async Task<HttpClient> RegisterClientAsync()
    {
        var client = _factory.CreateClient();
        var email = $"goals_{Guid.NewGuid()}@test.com";
        var resp = await client.PostAsync("/auth/register",
            new StringContent(JsonSerializer.Serialize(new { email, password = "Password1!" }),
                Encoding.UTF8, "application/json"));
        var token = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement.GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static StringContent GoalJson(int calories = 2000) => new(
        JsonSerializer.Serialize(new { goalType = 0, dailyCalorieTarget = calories }), // 0 = Cut
        Encoding.UTF8, "application/json");

    [Fact]
    public async Task Goals_Upsert_CreatesGoal()
    {
        var client = await RegisterClientAsync();
        var resp = await client.PutAsync("/goals", GoalJson());
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(2000, body.RootElement.GetProperty("dailyCalorieTarget").GetInt32());
    }

    [Fact]
    public async Task Goals_Upsert_ReplacesExistingGoal()
    {
        var client = await RegisterClientAsync();
        await client.PutAsync("/goals", GoalJson(1800));
        var resp = await client.PutAsync("/goals", GoalJson(2200));
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(2200, body.RootElement.GetProperty("dailyCalorieTarget").GetInt32());

        // Verify only one goal exists
        var getResp = await client.GetAsync("/goals");
        var getBody = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync());
        Assert.Equal(2200, getBody.RootElement.GetProperty("dailyCalorieTarget").GetInt32());
    }
}
