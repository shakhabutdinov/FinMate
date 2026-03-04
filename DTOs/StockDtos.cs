namespace aspnet.DTOs;

public record StockPortfolioDto(
    decimal TotalBalance,
    decimal ChangeAmount,
    decimal ChangePercent,
    List<StockHoldingDto> Holdings,
    List<TrendingStockDto> Trending
);

public record StockHoldingDto(
    Guid Id,
    string Symbol,
    string CompanyName,
    decimal PricePerShare,
    int Quantity,
    decimal TotalValue,
    string Color
);

public record TrendingStockDto(
    string Symbol,
    decimal Price,
    decimal ChangePercent,
    List<decimal> ChartData
);

public record CreateStockHoldingDto(string Symbol, string CompanyName, decimal PricePerShare, int Quantity, string Color);
