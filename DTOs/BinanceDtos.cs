namespace aspnet.DTOs;

public record ConnectBinanceDto(string ApiKey, string SecretKey);

public record BinanceStatusDto(bool IsConnected, DateTime? ConnectedAt);

public record BinanceAccountDataDto(
    bool IsConnected,
    DateTime? ConnectedAt,
    decimal TotalBalanceUsdt,
    List<BinanceAssetDto> Assets
);

public record BinanceAssetDto(
    string Asset,
    decimal Free,
    decimal Locked,
    decimal Total,
    decimal UsdtValue
);

public record KlinePointDto(long Time, decimal Close);
