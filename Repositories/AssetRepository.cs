using Microsoft.EntityFrameworkCore;
using aspnet.Data;
using aspnet.Models;
using aspnet.Repositories.Interfaces;

namespace aspnet.Repositories;

public class AssetRepository(FinMateDbContext context) : IAssetRepository
{
    public async Task<List<Asset>> GetByUserIdAsync(Guid userId) =>
        await context.Assets.Where(a => a.UserId == userId).ToListAsync();

    public async Task<Asset?> GetByIdAsync(Guid id) =>
        await context.Assets.FindAsync(id);

    public async Task<Asset> CreateAsync(Asset asset)
    {
        context.Assets.Add(asset);
        await context.SaveChangesAsync();
        return asset;
    }

    public async Task UpdateAsync(Asset asset)
    {
        context.Assets.Update(asset);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var asset = await context.Assets.FindAsync(id);
        if (asset != null)
        {
            context.Assets.Remove(asset);
            await context.SaveChangesAsync();
        }
    }
}
