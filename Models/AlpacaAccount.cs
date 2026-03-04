using System.ComponentModel.DataAnnotations;

namespace aspnet.Models;

public class AlpacaAccount
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    [Required, MaxLength(200)]
    public string ApiKey { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string SecretKey { get; set; } = string.Empty;

    public bool IsPaper { get; set; } = true;
    public bool IsConnected { get; set; }
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
