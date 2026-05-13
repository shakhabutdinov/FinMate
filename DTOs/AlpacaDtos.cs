namespace aspnet.DTOs;

public record ConnectAlpacaDto(string ApiKey, string SecretKey, bool IsPaper = true);

public record AlpacaStatusDto(bool IsConnected, DateTime? ConnectedAt, bool IsPaper);

public record AlpacaAccountDataDto(
    bool IsConnected,
    DateTime? ConnectedAt,
    decimal Equity,
    decimal Cash,
    decimal BuyingPower,
    decimal LongMarketValue,
    string AccountStatus,
    bool IsPaper,
    List<AlpacaPositionDto> Positions
);

public record AlpacaPositionDto(
    string Symbol,
    decimal Qty,
    decimal AvgEntryPrice,
    decimal CurrentPrice,
    decimal MarketValue,
    decimal UnrealizedPl,
    decimal UnrealizedPlPc,
    decimal ChangeToday
);

public record AlpacaBarDto(long Time, decimal Close);

// ── Order placement ────────────────────────────────────────────────────────

/// <summary>Request body for placing a buy or sell order.</summary>
/// <param name="Symbol">Stock ticker, e.g. "AAPL"</param>
/// <param name="Qty">Number of shares (fractional allowed)</param>
/// <param name="Side">"buy" or "sell"</param>
/// <param name="Type">"market" or "limit"</param>
/// <param name="LimitPrice">Required when Type = "limit"</param>
/// <param name="TimeInForce">"day" (default) or "gtc" (good till cancelled)</param>
public record PlaceOrderDto(
    string Symbol,
    decimal Qty,
    string Side,
    string Type = "market",
    decimal? LimitPrice = null,
    string TimeInForce = "day"
);

/// <summary>Returned after an order is accepted by Alpaca.</summary>
public record OrderResultDto(
    string OrderId,
    string Symbol,
    decimal Qty,
    string Side,
    string Type,
    string Status,
    decimal? FilledAvgPrice,
    decimal? LimitPrice,
    string TimeInForce,
    DateTime SubmittedAt
);

/// <summary>Summary of one order in the recent order history list.</summary>
public record OrderSummaryDto(
    string OrderId,
    string Symbol,
    decimal Qty,
    decimal FilledQty,
    string Side,
    string Type,
    string Status,
    decimal? FilledAvgPrice,
    DateTime SubmittedAt
);