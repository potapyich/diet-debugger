namespace DietDebugger.Domain.Entities;

public enum Sex { Male, Female, Other }
public enum DietType { Standard, Vegetarian, Vegan, Keto, Other }
public enum FeedbackTone { Neutral, Direct, Harsh }

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Profile fields
    public decimal? WeightKg { get; set; }
    public int? HeightCm { get; set; }
    public int? Age { get; set; }
    public Sex? Sex { get; set; }
    public DietType? DietType { get; set; }
    public FeedbackTone FeedbackTone { get; set; } = FeedbackTone.Neutral;
    public string PreferredLanguage { get; set; } = "en";
    public DateTime? ProfileCompletedAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
