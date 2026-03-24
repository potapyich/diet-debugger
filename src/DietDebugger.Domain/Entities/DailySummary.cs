namespace DietDebugger.Domain.Entities;

public class DailySummary
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }
    public decimal TotalCalories { get; set; }
    public decimal TotalProteinG { get; set; }
    public decimal TotalFatG { get; set; }
    public decimal TotalCarbsG { get; set; }
    public string Insights { get; set; } = "[]";
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public bool IsStale { get; set; }
}
