using Microsoft.EntityFrameworkCore;
using aspnet.Data;
using aspnet.Models;
using aspnet.Repositories.Interfaces;

namespace aspnet.Repositories;

public class ChatMessageRepository(FinMateDbContext context) : IChatMessageRepository
{
    public async Task<List<ChatMessage>> GetByUserIdAsync(Guid userId, int limit = 50) =>
        await context.ChatMessages
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

    public async Task<ChatMessage> CreateAsync(ChatMessage message)
    {
        context.ChatMessages.Add(message);
        await context.SaveChangesAsync();
        return message;
    }

    public async Task DeleteAllByUserIdAsync(Guid userId)
    {
        var messages = context.ChatMessages.Where(m => m.UserId == userId);
        context.ChatMessages.RemoveRange(messages);
        await context.SaveChangesAsync();
    }
}
