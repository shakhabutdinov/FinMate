using System.ComponentModel.DataAnnotations;

namespace aspnet.Models;

public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public bool IsFromAI { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
