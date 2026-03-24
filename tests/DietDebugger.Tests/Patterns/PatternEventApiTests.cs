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

namespace DietDebugger.Tests.Patterns;

[Trait("Category", "PatternEventApi")]
public class PatternEventApiTests : IClassFixture<PatternEventApiTests.AppFactory>
{
    private readonly AppFactory _factory;
    public PatternEventApiTests(AppFactory factory) => _factory = factory;

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
                    opts.UseInMemoryDatabase("PatternEventTest").UseInternalServiceProvider(sp));
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

    private async Task<(HttpClient client, Guid userId)> RegisterAsync()
    {
        var client = _factory.CreateClient();
        var email = $"pattern_{Guid.NewGuid()}@test.com";
        var resp = await client.PostAsync("/auth/register",
            new StringContent(JsonSerializer.Serialize(new { email, password = "Password1!" }),
                Encoding.UTF8, "application/json"));
        var body = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var token = body.RootElement.GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Get the user ID from the DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == email);
        return (client, user.Id);
    }

    private async Task SeedPatternEvents(Guid userId, int count, bool acknowledged = false)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        for (int i = 0; i < count; i++)
        {
            db.PatternEvents.Add(new PatternEvent
            {
                UserId = userId,
                PatternKey = $"test_pattern_{i}",
                Type = i % 2 == 0 ? PatternEventType.Warning : PatternEventType.Positive,
                Message = $"Test pattern {i}",
                Acknowledged = acknowledged,
                TriggeredAt = DateTime.UtcNow.AddHours(-i)
            });
        }
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task PatternEvents_UnacknowledgedFirst()
    {
        var (client, userId) = await RegisterAsync();
        // Add 2 acknowledged and 2 unacknowledged
        await SeedPatternEvents(userId, 2, acknowledged: false);
        await SeedPatternEvents(userId, 2, acknowledged: true);

        var resp = await client.GetAsync("/pattern-events?limit=10");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var events = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        Assert.True(events.GetArrayLength() == 4);

        // First events should be unacknowledged
        Assert.False(events[0].GetProperty("acknowledged").GetBoolean());
        Assert.False(events[1].GetProperty("acknowledged").GetBoolean());
    }

    [Fact]
    public async Task PatternEvents_Acknowledge_Works()
    {
        var (client, userId) = await RegisterAsync();
        await SeedPatternEvents(userId, 1, acknowledged: false);

        var list = JsonDocument.Parse(await (await client.GetAsync("/pattern-events")).Content.ReadAsStringAsync());
        var id = list.RootElement[0].GetProperty("id").GetString();

        var ack = await client.PostAsync($"/pattern-events/{id}/acknowledge", null);
        Assert.Equal(HttpStatusCode.OK, ack.StatusCode);
    }
}
