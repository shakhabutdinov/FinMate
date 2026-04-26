namespace aspnet.DTOs;

public record PfmOverviewDto(
    decimal IncomeMtd,
    decimal ExpensesMtd,
    List<CashflowDataDto> CashflowData,
    List<ExpenseSegmentDto> ExpenseSegments,
    decimal TotalExpenses
);

public record CashflowDataDto(string Month, decimal Income, decimal Expenses);

public record ExpenseSegmentDto(string Category, decimal Amount, string Color);

public record TransactionDto(
    Guid Id,
    string Type,
    string Category,
    decimal Amount,
    string Description,
    DateTime Date
);

public record CreateTransactionDto(string Type, string Category, decimal Amount, string? Description, DateTime? Date);

public record GoalDto(
    Guid Id,
    string Name,
    decimal TargetAmount,
    decimal CurrentAmount,
    DateTime? Deadline,
    decimal ProgressPercent
);

public record CreateGoalDto(string Name, decimal TargetAmount, decimal CurrentAmount, DateTime? Deadline);

public record ContributeGoalDto(decimal Amount);
