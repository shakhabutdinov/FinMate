using aspnet.Models;

namespace aspnet.Repositories.Interfaces;

public interface IFinancialGoalRepository
{
    Task<List<FinancialGoal>> GetByUserIdAsync(Guid userId);
    Task<FinancialGoal?> GetByIdAsync(Guid id);
    Task<FinancialGoal> CreateAsync(FinancialGoal goal);
    Task UpdateAsync(FinancialGoal goal);
    Task DeleteAsync(Guid id);
}
