namespace aspnet.DTOs;

public record DashboardDto(
    decimal TotalBalance,
    decimal ChangePercent,
    decimal ChangeAmount,
    List<AssetDto> Assets,
    List<QuickActionDto> QuickActions
);

public record AssetDto(
    Guid Id,
    string Name,
    string Type,
    decimal Balance,
    decimal ChangePercent,
    string Icon,
    List<decimal> SparklineData
);

public record QuickActionDto(string Name, string Icon);
