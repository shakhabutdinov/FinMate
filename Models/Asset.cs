using System.ComponentModel.DataAnnotations;

namespace aspnet.Models;

public enum AssetType
{
    Savings,
    Stock,
    Crypto
}

public class Asset
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public AssetType Type { get; set; }
    public decimal Balance { get; set; }
    public decimal ChangePercent { get; set; }

    [MaxLength(50)]
    public string Icon { get; set; } = string.Empty;

    public User User { get; set; } = null!;
    public ICollection<AssetSnapshot> Snapshots { get; set; } = [];
}
