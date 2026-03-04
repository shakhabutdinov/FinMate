using System.ComponentModel.DataAnnotations;

namespace aspnet.Models;

public class FinancialGoal
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public DateTime? Deadline { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
