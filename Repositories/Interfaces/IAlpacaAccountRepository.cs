using aspnet.Models;

namespace aspnet.Repositories.Interfaces;

public interface IAlpacaAccountRepository
{
    Task<AlpacaAccount?> GetByUserIdAsync(Guid userId);
    Task<AlpacaAccount> CreateAsync(AlpacaAccount account);
    Task UpdateAsync(AlpacaAccount account);
    Task DeleteAsync(Guid userId);
}
