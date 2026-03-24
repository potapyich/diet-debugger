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

[Trait("Category", "MealAnalysisApi")]
public class MealAnalysisApiTests : IClassFixture<MealAnalysisApiTests.AppFactory>
{
    private readonly AppFactory _factory;

    public MealAnalysisApiTests(AppFactory factory)
    {
        _factory = factory;
    }

    public class AppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove all DbContext registrations to avoid provider conflicts
                var toRemove = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    d.ServiceType == typeof(AppDbContext) ||
                    (d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>)) ||
                    d.ServiceType.FullName == "Microsoft.EntityFrameworkCore.Storage.IDatabaseProvider" ||
                    d.ServiceType.FullName == "Microsoft.EntityFrameworkCore.Infrastructure.IDatabaseFacadeDependencies"
                ).ToList();
                foreach (var d in toRemove) services.Remove(d);

                services.AddEntityFrameworkInMemoryDatabase();
                services.AddDbContext<AppDbContext>((sp, opts) =>
                    opts.UseInMemoryDatabase("MealAnalysisTest")
                        .UseInternalServiceProvider(sp));

                // Replace IFoodVisionAgent with stub
                var agentDesc = services.SingleOrDefault(d => d.ServiceType == typeof(IFoodVisionAgent));
                if (agentDesc != null) services.Remove(agentDesc);
                services.AddSingleton<IFoodVisionAgent, StubFoodVisionAgent>();
            });
        }
    }

    public class StubFoodVisionAgent : IFoodVisionAgent
    {
        public Task<FoodVisionResult> AnalyzeAsync(FoodVisionInput input, CancellationToken ct = default)
        {
            var result = new FoodVisionResult(
                [new FoodVisionIngredient("chicken", "200g")],
                400, 35, 15, 10, "one serving", 0.9);
            return Task.FromResult(result);
        }
    }

    private async Task<string> LoginAndGetTokenAsync(HttpClient client, string email = "analysis@test.com")
    {
        var register = await client.PostAsync("/auth/register",
            new StringContent(
                JsonSerializer.Serialize(new { email, password = "Password1!" }),
                Encoding.UTF8, "application/json"));
        var body = await register.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException($"Register returned empty body, status: {register.StatusCode}");
        if (!register.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Register failed: {register.StatusCode} - {body}");
            var login = await client.PostAsync("/auth/login",
                new StringContent(
                    JsonSerializer.Serialize(new { email, password = "Password1!" }),
                    Encoding.UTF8, "application/json"));
            body = await login.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
                throw new InvalidOperationException($"Login returned empty body, status: {login.StatusCode}");
        }
        var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("accessToken").GetString()!;
    }

    [Fact]
    public async Task PostAnalyze_WithText_ReturnsJobId()
    {
        var client = _factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client, $"analyze_{Guid.NewGuid()}@test.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsync("/meals/analyze",
            new StringContent(JsonSerializer.Serialize(new { text = "chicken salad" }),
                Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("jobId", body);
    }

    [Fact]
    public async Task GetAnalyzeStatus_ReturnsStatus()
    {
        var client = _factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client, $"status_{Guid.NewGuid()}@test.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var postResp = await client.PostAsync("/meals/analyze",
            new StringContent(JsonSerializer.Serialize(new { text = "oatmeal" }),
                Encoding.UTF8, "application/json"));
        var postBody = JsonDocument.Parse(await postResp.Content.ReadAsStringAsync());
        var jobId = postBody.RootElement.GetProperty("jobId").GetString();

        // Poll until job is found (up to 2 seconds)
        HttpResponseMessage? response = null;
        for (int i = 0; i < 10; i++)
        {
            response = await client.GetAsync($"/meals/analyze/{jobId}");
            if (response.StatusCode != HttpStatusCode.NotFound) break;
            await Task.Delay(200);
        }
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("status", body);
    }
}
