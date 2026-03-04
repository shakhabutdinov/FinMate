using aspnet.Models;

namespace aspnet.Repositories.Interfaces;

public interface IAssetRepository
{
    Task<List<Asset>> GetByUserIdAsync(Guid userId);
    Task<Asset?> GetByIdAsync(Guid id);
    Task<Asset> CreateAsync(Asset asset);
    Task UpdateAsync(Asset asset);
    Task DeleteAsync(Guid id);
}
