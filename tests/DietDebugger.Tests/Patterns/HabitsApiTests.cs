using System.Net;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using DietDebugger.Application.Interfaces;
using DietDebugger.Domain.Entities;
using DietDebugger.Infrastructure.AI;
using DietDebugger.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DietDebugger.Tests.Patterns;

[Trait("Category", "HabitsApi")]
public class HabitsApiTests : IClassFixture<HabitsApiTests.AppFactory>
{
    private readonly AppFactory _factory;
    public HabitsApiTests(AppFactory factory) => _factory = factory;

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
                    opts.UseInMemoryDatabase("HabitsTest").UseInternalServiceProvider(sp));
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

    private async Task<HttpClient> RegisterAsync()
    {
        var client = _factory.CreateClient();
        var email = $"habit_{Guid.NewGuid()}@test.com";
        var resp = await client.PostAsync("/auth/register",
            new StringContent(JsonSerializer.Serialize(new { email, password = "Password1!" }),
                Encoding.UTF8, "application/json"));
        var body = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var token = body.RootElement.GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task CreateHabit_Returns201()
    {
        var client = await RegisterAsync();
        var resp = await client.PostAsync("/habits",
            new StringContent(JsonSerializer.Serialize(new { habitDescription = "I snack when stressed" }),
                Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var body = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal("I snack when stressed", body.RootElement.GetProperty("habitDescription").GetString());
    }

    [Fact]
    public async Task GetHabits_ReturnsCreatedHabits()
    {
        var client = await RegisterAsync();
        await client.PostAsync("/habits",
            new StringContent(JsonSerializer.Serialize(new { habitDescription = "Habit A" }),
                Encoding.UTF8, "application/json"));
        await client.PostAsync("/habits",
            new StringContent(JsonSerializer.Serialize(new { habitDescription = "Habit B" }),
                Encoding.UTF8, "application/json"));

        var resp = await client.GetAsync("/habits");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(2, body.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task DeleteHabit_Returns204()
    {
        var client = await RegisterAsync();
        var create = await client.PostAsync("/habits",
            new StringContent(JsonSerializer.Serialize(new { habitDescription = "Habit to delete" }),
                Encoding.UTF8, "application/json"));
        var body = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = body.RootElement.GetProperty("id").GetString();

        var del = await client.DeleteAsync($"/habits/{id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var list = JsonDocument.Parse(await (await client.GetAsync("/habits")).Content.ReadAsStringAsync());
        Assert.Equal(0, list.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task CreateHabit_Rejects_WhenLimitExceeded()
    {
        var client = await RegisterAsync();
        for (int i = 0; i < 10; i++)
        {
            await client.PostAsync("/habits",
                new StringContent(JsonSerializer.Serialize(new { habitDescription = $"Habit {i}" }),
                    Encoding.UTF8, "application/json"));
        }

        var resp = await client.PostAsync("/habits",
            new StringContent(JsonSerializer.Serialize(new { habitDescription = "One too many" }),
                Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateHabit_Rejects_EmptyDescription()
    {
        var client = await RegisterAsync();
        var resp = await client.PostAsync("/habits",
            new StringContent(JsonSerializer.Serialize(new { habitDescription = "" }),
                Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}
