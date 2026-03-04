using Microsoft.EntityFrameworkCore;
using aspnet.Data;
using aspnet.Models;
using aspnet.Repositories.Interfaces;

namespace aspnet.Repositories;

public class FinancialGoalRepository(FinMateDbContext context) : IFinancialGoalRepository
{
    public async Task<List<FinancialGoal>> GetByUserIdAsync(Guid userId) =>
        await context.FinancialGoals.Where(g => g.UserId == userId).ToListAsync();

    public async Task<FinancialGoal?> GetByIdAsync(Guid id) =>
        await context.FinancialGoals.FindAsync(id);

    public async Task<FinancialGoal> CreateAsync(FinancialGoal goal)
    {
        context.FinancialGoals.Add(goal);
        await context.SaveChangesAsync();
        return goal;
    }

    public async Task UpdateAsync(FinancialGoal goal)
    {
        context.FinancialGoals.Update(goal);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var goal = await context.FinancialGoals.FindAsync(id);
        if (goal != null)
        {
            context.FinancialGoals.Remove(goal);
            await context.SaveChangesAsync();
        }
    }
}
