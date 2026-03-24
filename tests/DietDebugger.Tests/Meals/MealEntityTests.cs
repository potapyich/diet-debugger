using DietDebugger.Domain.Entities;
using DietDebugger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DietDebugger.Tests.Meals;

[Trait("Category", "MealEntity")]
public class MealEntityTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Meal_CanBeSavedAndRetrieved()
    {
        using var db = CreateDb();
        var user = new User { Email = "test@example.com", PasswordHash = "hash" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var meal = new Meal
        {
            UserId = user.Id,
            Calories = 500m,
            ProteinG = 30m,
            FatG = 20m,
            CarbsG = 50m,
            Ingredients = """[{"name":"chicken","amount":"200g"}]""",
            PortionEstimate = "medium plate",
            ConfidenceScore = 0.85m,
            Source = MealSource.Photo,
        };
        db.Meals.Add(meal);
        await db.SaveChangesAsync();

        var saved = await db.Meals.FindAsync(meal.Id);
        Assert.NotNull(saved);
        Assert.Equal(500m, saved.Calories);
        Assert.Equal(MealSource.Photo, saved.Source);
        Assert.Null(saved.Notes);
    }

    [Fact]
    public async Task Meal_AllSources_AreValid()
    {
        using var db = CreateDb();
        var user = new User { Email = "src@example.com", PasswordHash = "hash" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        foreach (var source in Enum.GetValues<MealSource>())
        {
            var meal = new Meal { UserId = user.Id, Source = source };
            db.Meals.Add(meal);
        }
        await db.SaveChangesAsync();

        var count = await db.Meals.CountAsync();
        Assert.Equal(3, count);
    }
}
