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
