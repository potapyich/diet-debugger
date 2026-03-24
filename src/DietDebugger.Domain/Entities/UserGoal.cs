namespace DietDebugger.Domain.Entities;

public enum GoalType { Cut, Maintain, Bulk }

public class UserGoal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public GoalType GoalType { get; set; }
    public int DailyCalorieTarget { get; set; }
    public int? ProteinTargetG { get; set; }
    public int? FatTargetG { get; set; }
    public int? CarbsTargetG { get; set; }
    public DateTime ActiveSince { get; set; } = DateTime.UtcNow;
}
