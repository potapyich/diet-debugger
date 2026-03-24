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

[Trait("Category", "ProfileApi")]
public class ProfileApiTests : IClassFixture<ProfileApiTests.AppFactory>
{
    private readonly AppFactory _factory;
    public ProfileApiTests(AppFactory factory) => _factory = factory;

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
                    opts.UseInMemoryDatabase("ProfileApiTest").UseInternalServiceProvider(sp));
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

    private async Task<(HttpClient client, string token)> RegisterAsync()
    {
        var client = _factory.CreateClient();
        var email = $"profile_{Guid.NewGuid()}@test.com";
        var resp = await client.PostAsync("/auth/register",
            new StringContent(JsonSerializer.Serialize(new { email, password = "Password1!" }),
                Encoding.UTF8, "application/json"));
        var body = await resp.Content.ReadAsStringAsync();
        var token = JsonDocument.Parse(body).RootElement.GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, token);
    }

    [Fact]
    public async Task GetProfile_ReturnsProfile()
    {
        var (client, _) = await RegisterAsync();
        var resp = await client.GetAsync("/profile");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_SetsFields()
    {
        var (client, _) = await RegisterAsync();
        var resp = await client.PutAsync("/profile",
            new StringContent(JsonSerializer.Serialize(new
            {
                weightKg = 75.5,
                heightCm = 178,
                age = 30,
                sex = 0, // Male
                preferredLanguage = "ru"
            }), Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(75.5, body.RootElement.GetProperty("weightKg").GetDouble());
    }

    [Fact]
    public async Task Profile_AllFieldsFilled_SetsProfileCompletedAt()
    {
        var (client, _) = await RegisterAsync();
        var resp = await client.PutAsync("/profile",
            new StringContent(JsonSerializer.Serialize(new
            {
                weightKg = 70, heightCm = 175, age = 28, sex = 0
            }), Encoding.UTF8, "application/json"));
        var body = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.NotEqual(JsonValueKind.Null, body.RootElement.GetProperty("profileCompletedAt").ValueKind);
    }

    [Fact]
    public async Task Profile_MissingField_DoesNotSetProfileCompletedAt()
    {
        var (client, _) = await RegisterAsync();
        // Set only weight, not all required fields
        var resp = await client.PutAsync("/profile",
            new StringContent(JsonSerializer.Serialize(new { weightKg = 70 }),
                Encoding.UTF8, "application/json"));
        var body = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("profileCompletedAt").ValueKind);
    }
}
