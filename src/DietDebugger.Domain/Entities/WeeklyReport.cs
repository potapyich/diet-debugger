namespace DietDebugger.Domain.Entities;

public class WeeklyReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public string Patterns { get; set; } = "[]";
    public string CalendarDayStatuses { get; set; } = "[]";
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public bool IsStale { get; set; }
}
