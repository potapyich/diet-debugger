using System.Text;
using DietDebugger.Api.Endpoints;
using DietDebugger.Api.Validation;
using DietDebugger.Application;
using DietDebugger.Infrastructure;
using DietDebugger.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddValidators();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

var jwtSecret = builder.Configuration["JWT_SECRET"]
    ?? builder.Configuration["JwtSettings:Secret"]
    ?? "dev-secret-change-in-production-min-32-chars";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", async (AppDbContext db) =>
{
    try
    {
        await db.Database.CanConnectAsync();
        return Results.Ok(new { status = "ok", db = "connected" });
    }
    catch
    {
        return Results.Ok(new { status = "ok", db = "disconnected" });
    }
});

app.MapAuthEndpoints();
app.MapMealAnalysisEndpoints();
app.MapMealEndpoints();
app.MapDailySummaryEndpoints();
app.MapProfileEndpoints();
app.MapGoalsEndpoints();
app.MapWeeklyReportEndpoints();
app.MapPatternEventEndpoints();
app.MapHabitsEndpoints();
app.MapMonthlyReportEndpoints();

app.Run();

public partial class Program { }
