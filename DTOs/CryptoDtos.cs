namespace aspnet.DTOs;

public record CryptoPortfolioDto(
    decimal TotalBalance,
    decimal ChangeAmount,
    decimal ChangePercent,
    List<CryptoHoldingDto> Holdings,
    List<TrendingCryptoDto> Trending
);

public record CryptoHoldingDto(
    Guid Id,
    string Symbol,
    string Name,
    decimal PricePerUnit,
    decimal Amount,
    decimal TotalValue,
    string Color
);

public record TrendingCryptoDto(
    string Symbol,
    decimal Price,
    decimal ChangePercent,
    List<decimal> ChartData
);

public record CreateCryptoHoldingDto(string Symbol, string Name, decimal PricePerUnit, decimal Amount, string Color);
