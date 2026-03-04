using System.ComponentModel.DataAnnotations;

namespace aspnet.Models;

public enum TransactionType
{
    Income,
    Expense
}

public class Transaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public TransactionType Type { get; set; }

    [Required, MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public DateTime Date { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
