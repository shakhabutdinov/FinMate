using Microsoft.EntityFrameworkCore;
using aspnet.Data;
using aspnet.Models;
using aspnet.Repositories.Interfaces;

namespace aspnet.Repositories;

public class CryptoHoldingRepository(FinMateDbContext context) : ICryptoHoldingRepository
{
    public async Task<List<CryptoHolding>> GetByUserIdAsync(Guid userId) =>
        await context.CryptoHoldings.Where(c => c.UserId == userId).ToListAsync();

    public async Task<CryptoHolding?> GetByIdAsync(Guid id) =>
        await context.CryptoHoldings.FindAsync(id);

    public async Task<CryptoHolding> CreateAsync(CryptoHolding holding)
    {
        context.CryptoHoldings.Add(holding);
        await context.SaveChangesAsync();
        return holding;
    }

    public async Task UpdateAsync(CryptoHolding holding)
    {
        context.CryptoHoldings.Update(holding);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var holding = await context.CryptoHoldings.FindAsync(id);
        if (holding != null)
        {
            context.CryptoHoldings.Remove(holding);
            await context.SaveChangesAsync();
        }
    }
}
