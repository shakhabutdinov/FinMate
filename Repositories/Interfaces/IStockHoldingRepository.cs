using aspnet.Models;

namespace aspnet.Repositories.Interfaces;

public interface IStockHoldingRepository
{
    Task<List<StockHolding>> GetByUserIdAsync(Guid userId);
    Task<StockHolding?> GetByIdAsync(Guid id);
    Task<StockHolding> CreateAsync(StockHolding holding);
    Task UpdateAsync(StockHolding holding);
    Task DeleteAsync(Guid id);
}
