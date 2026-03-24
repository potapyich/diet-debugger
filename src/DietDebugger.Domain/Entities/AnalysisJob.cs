namespace DietDebugger.Domain.Entities;

public enum AnalysisJobStatus { Pending, Ready, Failed }

public class AnalysisJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public AnalysisJobStatus Status { get; set; } = AnalysisJobStatus.Pending;
    public string Input { get; set; } = string.Empty;
    public string? Result { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
