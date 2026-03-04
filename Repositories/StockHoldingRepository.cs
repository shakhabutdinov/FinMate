using Microsoft.EntityFrameworkCore;
using aspnet.Data;
using aspnet.Models;
using aspnet.Repositories.Interfaces;

namespace aspnet.Repositories;

public class StockHoldingRepository(FinMateDbContext context) : IStockHoldingRepository
{
    public async Task<List<StockHolding>> GetByUserIdAsync(Guid userId) =>
        await context.StockHoldings.Where(s => s.UserId == userId).ToListAsync();

    public async Task<StockHolding?> GetByIdAsync(Guid id) =>
        await context.StockHoldings.FindAsync(id);

    public async Task<StockHolding> CreateAsync(StockHolding holding)
    {
        context.StockHoldings.Add(holding);
        await context.SaveChangesAsync();
        return holding;
    }

    public async Task UpdateAsync(StockHolding holding)
    {
        context.StockHoldings.Update(holding);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var holding = await context.StockHoldings.FindAsync(id);
        if (holding != null)
        {
            context.StockHoldings.Remove(holding);
            await context.SaveChangesAsync();
        }
    }
}
