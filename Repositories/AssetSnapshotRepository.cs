using Microsoft.EntityFrameworkCore;
using aspnet.Data;
using aspnet.Models;
using aspnet.Repositories.Interfaces;

namespace aspnet.Repositories;

public class AssetSnapshotRepository(FinMateDbContext context) : IAssetSnapshotRepository
{
    public async Task<List<AssetSnapshot>> GetByAssetIdAsync(Guid assetId, int days = 7)
    {
        var since = DateTime.UtcNow.Date.AddDays(-days);
        return await context.AssetSnapshots
            .Where(s => s.AssetId == assetId && s.Date >= since)
            .OrderBy(s => s.Date)
            .ToListAsync();
    }

    public async Task<bool> ExistsForTodayAsync(Guid assetId)
    {
        var today = DateTime.UtcNow.Date;
        return await context.AssetSnapshots
            .AnyAsync(s => s.AssetId == assetId && s.Date == today);
    }

    public async Task CreateAsync(AssetSnapshot snapshot)
    {
        context.AssetSnapshots.Add(snapshot);
        await context.SaveChangesAsync();
    }

    public async Task CreateRangeAsync(List<AssetSnapshot> snapshots)
    {
        context.AssetSnapshots.AddRange(snapshots);
        await context.SaveChangesAsync();
    }
}
