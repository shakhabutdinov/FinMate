using Microsoft.EntityFrameworkCore;
using aspnet.Data;
using aspnet.Models;
using aspnet.Repositories.Interfaces;

namespace aspnet.Repositories;

public class BinanceAccountRepository(FinMateDbContext context) : IBinanceAccountRepository
{
    public async Task<BinanceAccount?> GetByUserIdAsync(Guid userId) =>
        await context.BinanceAccounts.FirstOrDefaultAsync(b => b.UserId == userId);

    public async Task<BinanceAccount> CreateAsync(BinanceAccount account)
    {
        context.BinanceAccounts.Add(account);
        await context.SaveChangesAsync();
        return account;
    }

    public async Task UpdateAsync(BinanceAccount account)
    {
        context.BinanceAccounts.Update(account);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid userId)
    {
        var account = await context.BinanceAccounts.FirstOrDefaultAsync(b => b.UserId == userId);
        if (account != null)
        {
            context.BinanceAccounts.Remove(account);
            await context.SaveChangesAsync();
        }
    }
}
