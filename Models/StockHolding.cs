using System.ComponentModel.DataAnnotations;

namespace aspnet.Models;

public class StockHolding
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    [Required, MaxLength(20)]
    public string Symbol { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    public decimal PricePerShare { get; set; }
    public int Quantity { get; set; }
    public decimal TotalValue => PricePerShare * Quantity;

    [MaxLength(50)]
    public string Color { get; set; } = string.Empty;

    public User User { get; set; } = null!;
}
