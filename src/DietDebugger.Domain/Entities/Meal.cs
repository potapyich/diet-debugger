namespace DietDebugger.Domain.Entities;

public enum MealSource { Photo, Text, Manual }

public class Meal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
    public decimal Calories { get; set; }
    public decimal ProteinG { get; set; }
    public decimal FatG { get; set; }
    public decimal CarbsG { get; set; }
    public string Ingredients { get; set; } = "[]";
    public string PortionEstimate { get; set; } = string.Empty;
    public decimal ConfidenceScore { get; set; }
    public MealSource Source { get; set; }
    public string? Notes { get; set; }
}
