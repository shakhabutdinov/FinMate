using aspnet.DTOs;
using aspnet.Models;
using aspnet.Repositories.Interfaces;

namespace aspnet.Services;

public class PfmService(ITransactionRepository transactionRepo, IFinancialGoalRepository goalRepo)
{
    private static readonly Dictionary<string, string> CategoryColors = new()
    {
        ["Housing"] = "#00FF88",
        ["Food"] = "#00CC6A",
        ["Transport"] = "#009950",
        ["Utilities"] = "#006635",
        ["Entertainment"] = "#004422"
    };

    public async Task<PfmOverviewDto> GetOverviewAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var transactions = await transactionRepo.GetByUserIdAsync(userId);

        var monthTransactions = transactions.Where(t => t.Date >= startOfMonth).ToList();
        var incomeMtd = monthTransactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var expensesMtd = monthTransactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

        var cashflowData = GetCashflowData(transactions);
        var expenseSegments = GetExpenseSegments(monthTransactions);
        var totalExpenses = expenseSegments.Sum(e => e.Amount);

        return new PfmOverviewDto(incomeMtd, expensesMtd, cashflowData, expenseSegments, totalExpenses);
    }

    public async Task<List<TransactionDto>> GetTransactionsAsync(Guid userId, int? limit = null)
    {
        var transactions = await transactionRepo.GetByUserIdAsync(userId, limit);
        return transactions.Select(t => new TransactionDto(
            t.Id, t.Type.ToString(), t.Category, t.Amount, t.Description, t.Date
        )).ToList();
    }

    public async Task<TransactionDto> CreateTransactionAsync(Guid userId, CreateTransactionDto dto)
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = Enum.Parse<TransactionType>(dto.Type),
            Category = dto.Category,
            Amount = dto.Amount,
            Description = dto.Description,
            Date = dto.Date ?? DateTime.UtcNow
        };

        await transactionRepo.CreateAsync(transaction);
        return new TransactionDto(transaction.Id, transaction.Type.ToString(), transaction.Category, transaction.Amount, transaction.Description, transaction.Date);
    }

    public async Task<List<GoalDto>> GetGoalsAsync(Guid userId)
    {
        var goals = await goalRepo.GetByUserIdAsync(userId);
        return goals.Select(g => new GoalDto(
            g.Id, g.Name, g.TargetAmount, g.CurrentAmount, g.Deadline,
            g.TargetAmount > 0 ? Math.Round(g.CurrentAmount / g.TargetAmount * 100, 1) : 0
        )).ToList();
    }

    public async Task<GoalDto> CreateGoalAsync(Guid userId, CreateGoalDto dto)
    {
        var goal = new FinancialGoal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = dto.Name,
            TargetAmount = dto.TargetAmount,
            CurrentAmount = dto.CurrentAmount,
            Deadline = dto.Deadline
        };

        await goalRepo.CreateAsync(goal);
        var progress = goal.TargetAmount > 0 ? Math.Round(goal.CurrentAmount / goal.TargetAmount * 100, 1) : 0;
        return new GoalDto(goal.Id, goal.Name, goal.TargetAmount, goal.CurrentAmount, goal.Deadline, progress);
    }

    private static List<CashflowDataDto> GetCashflowData(List<Transaction> transactions)
    {
        string[] months = ["Jan", "Feb", "Mar", "Apr", "May"];
        return months.Select((m, i) =>
        {
            var monthNum = i + 1;
            var monthTx = transactions.Where(t => t.Date.Month == monthNum).ToList();
            return new CashflowDataDto(
                m,
                monthTx.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                monthTx.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)
            );
        }).ToList();
    }

    private static List<ExpenseSegmentDto> GetExpenseSegments(List<Transaction> transactions)
    {
        return transactions
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.Category)
            .Select(g => new ExpenseSegmentDto(
                g.Key,
                g.Sum(t => t.Amount),
                CategoryColors.GetValueOrDefault(g.Key, "#00FF88")
            ))
            .OrderByDescending(e => e.Amount)
            .ToList();
    }
}
