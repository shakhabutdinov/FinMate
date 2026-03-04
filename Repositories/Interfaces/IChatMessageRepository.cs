using aspnet.Models;

namespace aspnet.Repositories.Interfaces;

public interface IChatMessageRepository
{
    Task<List<ChatMessage>> GetByUserIdAsync(Guid userId, int limit = 50);
    Task<ChatMessage> CreateAsync(ChatMessage message);
    Task DeleteAllByUserIdAsync(Guid userId);
}
