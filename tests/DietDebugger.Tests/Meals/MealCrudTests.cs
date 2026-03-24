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

namespace DietDebugger.Tests.Meals;

[Trait("Category", "MealCrud")]
public class MealCrudTests : IClassFixture<MealCrudTests.AppFactory>
{
    private readonly AppFactory _factory;

    public MealCrudTests(AppFactory factory) => _factory = factory;

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
                    opts.UseInMemoryDatabase("MealCrudTest")
                        .UseInternalServiceProvider(sp));

                var agentDesc = services.SingleOrDefault(d => d.ServiceType == typeof(IFoodVisionAgent));
                if (agentDesc != null) services.Remove(agentDesc);
                services.AddSingleton<IFoodVisionAgent, StubAgent>();
            });
        }

        public class StubAgent : IFoodVisionAgent
        {
            public Task<FoodVisionResult> AnalyzeAsync(FoodVisionInput input, CancellationToken ct = default) =>
                Task.FromResult(new FoodVisionResult([], 400, 35, 15, 10, "medium", 0.9));
        }
    }

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private async Task<(HttpClient client, string token)> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var email = $"crud_{Guid.NewGuid()}@test.com";
        var resp = await client.PostAsync("/auth/register",
            new StringContent(JsonSerializer.Serialize(new { email, password = "Password1!" }),
                Encoding.UTF8, "application/json"));
        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        var token = doc.RootElement.GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, token);
    }

    private static StringContent MealJson(decimal calories = 400) => new(
        JsonSerializer.Serialize(new
        {
            loggedAt = DateTime.UtcNow,
            calories,
            proteinG = 30,
            fatG = 15,
            carbsG = 50,
            confidenceScore = 0.9,
            source = 2 // Manual
        }),
        Encoding.UTF8, "application/json");

    [Fact]
    public async Task PostMeal_ReturnsSavedMeal()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var resp = await client.PostAsync("/meals", MealJson());
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("id", body);
        Assert.Contains("calories", body.ToLower());
    }

    [Fact]
    public async Task GetMeal_ReturnsCorrectMeal()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var post = await client.PostAsync("/meals", MealJson());
        var postBody = JsonDocument.Parse(await post.Content.ReadAsStringAsync());
        var id = postBody.RootElement.GetProperty("id").GetString();

        var resp = await client.GetAsync($"/meals/{id}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task DeleteMeal_RemovesMeal()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var post = await client.PostAsync("/meals", MealJson());
        var id = JsonDocument.Parse(await post.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetString();

        var del = await client.DeleteAsync($"/meals/{id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var get = await client.GetAsync($"/meals/{id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task ListMeals_ReturnsUserMeals()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        await client.PostAsync("/meals", MealJson());
        await client.PostAsync("/meals", MealJson());

        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var resp = await client.GetAsync($"/meals?date={today}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        var arr = JsonDocument.Parse(body).RootElement;
        Assert.True(arr.GetArrayLength() >= 2);
    }
}

[Trait("Category", "MealCrud")]
public class MealApiCrossUserTests : IClassFixture<MealCrudTests.AppFactory>
{
    private readonly MealCrudTests.AppFactory _factory;

    public MealApiCrossUserTests(MealCrudTests.AppFactory factory) => _factory = factory;

    private async Task<(HttpClient client, string token)> RegisterClient(string email)
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsync("/auth/register",
            new StringContent(JsonSerializer.Serialize(new { email, password = "Password1!" }),
                Encoding.UTF8, "application/json"));
        var body = await resp.Content.ReadAsStringAsync();
        var token = JsonDocument.Parse(body).RootElement.GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, token);
    }

    [Fact]
    public async Task MealApi_OtherUserMeal_Returns403()
    {
        var (client1, _) = await RegisterClient($"user1_{Guid.NewGuid()}@test.com");
        var (client2, _) = await RegisterClient($"user2_{Guid.NewGuid()}@test.com");

        // User 1 creates a meal
        var post = await client1.PostAsync("/meals",
            new StringContent(JsonSerializer.Serialize(new
            {
                calories = 300m, proteinG = 20m, fatG = 10m, carbsG = 30m,
                confidenceScore = 0.9m, source = 2
            }), Encoding.UTF8, "application/json"));
        var id = JsonDocument.Parse(await post.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetString();

        // User 2 tries to access user 1's meal
        var resp = await client2.GetAsync($"/meals/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }
}
