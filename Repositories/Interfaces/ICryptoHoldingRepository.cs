using aspnet.Models;

namespace aspnet.Repositories.Interfaces;

public interface ICryptoHoldingRepository
{
    Task<List<CryptoHolding>> GetByUserIdAsync(Guid userId);
    Task<CryptoHolding?> GetByIdAsync(Guid id);
    Task<CryptoHolding> CreateAsync(CryptoHolding holding);
    Task UpdateAsync(CryptoHolding holding);
    Task DeleteAsync(Guid id);
}
