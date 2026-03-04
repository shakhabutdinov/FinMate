using Microsoft.EntityFrameworkCore;
using aspnet.Data;
using aspnet.Models;
using aspnet.Repositories.Interfaces;

namespace aspnet.Repositories;

public class AlpacaAccountRepository(FinMateDbContext context) : IAlpacaAccountRepository
{
    public async Task<AlpacaAccount?> GetByUserIdAsync(Guid userId) =>
        await context.AlpacaAccounts.FirstOrDefaultAsync(a => a.UserId == userId);

    public async Task<AlpacaAccount> CreateAsync(AlpacaAccount account)
    {
        context.AlpacaAccounts.Add(account);
        await context.SaveChangesAsync();
        return account;
    }

    public async Task UpdateAsync(AlpacaAccount account)
    {
        context.AlpacaAccounts.Update(account);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid userId)
    {
        var account = await context.AlpacaAccounts.FirstOrDefaultAsync(a => a.UserId == userId);
        if (account != null)
        {
            context.AlpacaAccounts.Remove(account);
            await context.SaveChangesAsync();
        }
    }
}
