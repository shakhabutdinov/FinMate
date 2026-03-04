using Microsoft.EntityFrameworkCore;
using aspnet.Data;
using aspnet.Models;
using aspnet.Repositories.Interfaces;

namespace aspnet.Repositories;

public class TrendingItemRepository(FinMateDbContext context) : ITrendingItemRepository
{
    public async Task<List<TrendingItem>> GetByCategoryAsync(string category) =>
        await context.TrendingItems.Where(t => t.Category == category).ToListAsync();

    public async Task<List<TrendingItem>> GetAllAsync() =>
        await context.TrendingItems.ToListAsync();
}
