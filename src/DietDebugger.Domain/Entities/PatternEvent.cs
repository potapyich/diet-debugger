namespace DietDebugger.Domain.Entities;

public enum PatternEventType { Positive, Warning }

public class PatternEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string PatternKey { get; set; } = string.Empty;
    public PatternEventType Type { get; set; }
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    public bool Acknowledged { get; set; }
    public string Message { get; set; } = string.Empty;
}
