using aspnet.Models;

namespace aspnet.Repositories.Interfaces;

public interface ITrendingItemRepository
{
    Task<List<TrendingItem>> GetByCategoryAsync(string category);
    Task<List<TrendingItem>> GetAllAsync();
}
