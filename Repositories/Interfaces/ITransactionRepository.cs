using aspnet.Models;

namespace aspnet.Repositories.Interfaces;

public interface ITransactionRepository
{
    Task<List<Transaction>> GetByUserIdAsync(Guid userId, int? limit = null);
    Task<List<Transaction>> GetByUserIdAndDateRangeAsync(Guid userId, DateTime from, DateTime to);
    Task<Transaction?> GetByIdAsync(Guid id);
    Task<Transaction> CreateAsync(Transaction transaction);
    Task UpdateAsync(Transaction transaction);
    Task DeleteAsync(Guid id);
}
