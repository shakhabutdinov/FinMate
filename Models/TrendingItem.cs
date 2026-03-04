using System.ComponentModel.DataAnnotations;

namespace aspnet.Models;

public class TrendingItem
{
    public Guid Id { get; set; }

    [Required, MaxLength(20)]
    public string Symbol { get; set; } = string.Empty;

    public decimal Price { get; set; }
    public decimal ChangePercent { get; set; }

    [MaxLength(20)]
    public string Category { get; set; } = string.Empty; // "stock" or "crypto"

    public List<decimal> ChartData { get; set; } = [];
}
