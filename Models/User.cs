using System.ComponentModel.DataAnnotations;

namespace aspnet.Models;

public class User
{
    public Guid Id { get; set; }

    [Required, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(10)]
    public string Initials { get; set; } = string.Empty;

    public string? GoogleId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime TrialEndDate { get; set; } = DateTime.UtcNow.AddDays(30);
    public DateTime? SubscriptionEndDate { get; set; }

    public ICollection<Asset> Assets { get; set; } = [];
    public ICollection<StockHolding> StockHoldings { get; set; } = [];
    public ICollection<CryptoHolding> CryptoHoldings { get; set; } = [];
    public ICollection<Transaction> Transactions { get; set; } = [];
    public ICollection<FinancialGoal> FinancialGoals { get; set; } = [];
    public ICollection<ChatMessage> ChatMessages { get; set; } = [];
    public BinanceAccount? BinanceAccount { get; set; }
    public AlpacaAccount? AlpacaAccount { get; set; }
}
