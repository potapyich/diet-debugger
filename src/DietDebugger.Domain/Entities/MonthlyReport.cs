namespace DietDebugger.Domain.Entities;

public class MonthlyReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public DateOnly MonthStart { get; set; }  // Always the 1st of the month
    public string Assessment { get; set; } = string.Empty;  // jsonb
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
