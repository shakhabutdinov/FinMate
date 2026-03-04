using System.ComponentModel.DataAnnotations;

namespace aspnet.Models;

public class CryptoHolding
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    [Required, MaxLength(20)]
    public string Symbol { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public decimal PricePerUnit { get; set; }
    public decimal Amount { get; set; }
    public decimal TotalValue => PricePerUnit * Amount;

    [MaxLength(50)]
    public string Color { get; set; } = string.Empty;

    public User User { get; set; } = null!;
}
