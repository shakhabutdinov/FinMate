using aspnet.Models;

namespace aspnet.Repositories.Interfaces;

public interface IBinanceAccountRepository
{
    Task<BinanceAccount?> GetByUserIdAsync(Guid userId);
    Task<BinanceAccount> CreateAsync(BinanceAccount account);
    Task UpdateAsync(BinanceAccount account);
    Task DeleteAsync(Guid userId);
}
