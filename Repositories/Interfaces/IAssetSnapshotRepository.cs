using aspnet.Models;

namespace aspnet.Repositories.Interfaces;

public interface IAssetSnapshotRepository
{
    Task<List<AssetSnapshot>> GetByAssetIdAsync(Guid assetId, int days = 7);
    Task<bool> ExistsForTodayAsync(Guid assetId);
    Task CreateAsync(AssetSnapshot snapshot);
    Task CreateRangeAsync(List<AssetSnapshot> snapshots);
}
