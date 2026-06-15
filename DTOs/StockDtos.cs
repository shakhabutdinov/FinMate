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
    decimal Quantity,
    decimal TotalValue,
    string Color,
    decimal CostBasis = 0); 

public record TrendingStockDto(
    string Symbol,
    decimal Price,
    decimal ChangePercent,
    List<decimal> ChartData
);

public record CreateStockHoldingDto(string Symbol, string CompanyName, decimal PricePerShare, int Quantity, string Color);
