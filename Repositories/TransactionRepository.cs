using Microsoft.EntityFrameworkCore;
using aspnet.Data;
using aspnet.Models;
using aspnet.Repositories.Interfaces;

namespace aspnet.Repositories;

public class TransactionRepository(FinMateDbContext context) : ITransactionRepository
{
    public async Task<List<Transaction>> GetByUserIdAsync(Guid userId, int? limit = null)
    {
        var query = context.Transactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.Date)
            .AsQueryable();

        if (limit.HasValue)
            query = query.Take(limit.Value);

        return await query.ToListAsync();
    }

    public async Task<List<Transaction>> GetByUserIdAndDateRangeAsync(Guid userId, DateTime from, DateTime to) =>
        await context.Transactions
            .Where(t => t.UserId == userId && t.Date >= from && t.Date <= to)
            .OrderByDescending(t => t.Date)
            .ToListAsync();

    public async Task<Transaction?> GetByIdAsync(Guid id) =>
        await context.Transactions.FindAsync(id);

    public async Task<Transaction> CreateAsync(Transaction transaction)
    {
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();
        return transaction;
    }

    public async Task UpdateAsync(Transaction transaction)
    {
        context.Transactions.Update(transaction);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var transaction = await context.Transactions.FindAsync(id);
        if (transaction != null)
        {
            context.Transactions.Remove(transaction);
            await context.SaveChangesAsync();
        }
    }
}
