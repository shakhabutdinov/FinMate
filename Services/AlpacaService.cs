using System.Globalization;
using System.Text.Json;
using aspnet.DTOs;
using aspnet.Models;
using aspnet.Repositories.Interfaces;

namespace aspnet.Services;

public class AlpacaService(IAlpacaAccountRepository alpacaRepo)
{
    private const string LiveBaseUrl = "https://api.alpaca.markets";
    private const string PaperBaseUrl = "https://paper-api.alpaca.markets";
    private const string DataBaseUrl = "https://data.alpaca.markets";

    public async Task<AlpacaStatusDto> GetStatusAsync(Guid userId)
    {
        var account = await alpacaRepo.GetByUserIdAsync(userId);
        if (account == null)
            return new AlpacaStatusDto(false, null, true);

        return new AlpacaStatusDto(account.IsConnected, account.ConnectedAt, account.IsPaper);
    }

    public async Task<AlpacaAccountDataDto> ConnectAsync(Guid userId, ConnectAlpacaDto dto)
    {
        var accountData = await FetchAlpacaAccountData(dto.ApiKey, dto.SecretKey, dto.IsPaper);

        var existing = await alpacaRepo.GetByUserIdAsync(userId);
        if (existing != null)
        {
            existing.ApiKey = dto.ApiKey;
            existing.SecretKey = dto.SecretKey;
            existing.IsPaper = dto.IsPaper;
            existing.IsConnected = true;
            existing.ConnectedAt = DateTime.UtcNow;
            await alpacaRepo.UpdateAsync(existing);
        }
        else
        {
            var account = new AlpacaAccount
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ApiKey = dto.ApiKey,
                SecretKey = dto.SecretKey,
                IsPaper = dto.IsPaper,
                IsConnected = true,
                ConnectedAt = DateTime.UtcNow
            };
            await alpacaRepo.CreateAsync(account);
        }

        return accountData;
    }

    public async Task<AlpacaAccountDataDto> GetAccountDataAsync(Guid userId)
    {
        var account = await alpacaRepo.GetByUserIdAsync(userId);
        if (account == null || !account.IsConnected)
            return new AlpacaAccountDataDto(false, null, 0, 0, 0, 0, "DISCONNECTED", true, []);

        try
        {
            return await FetchAlpacaAccountData(account.ApiKey, account.SecretKey, account.IsPaper);
        }
        catch
        {
            return new AlpacaAccountDataDto(true, account.ConnectedAt, 0, 0, 0, 0, "ERROR", account.IsPaper, []);
        }
    }

    public async Task DisconnectAsync(Guid userId) =>
        await alpacaRepo.DeleteAsync(userId);

    public async Task<List<AlpacaBarDto>> GetBarsAsync(string symbol, string apiKey, string secretKey, string timeframe = "1Day", int limit = 30)
    {
        using var client = CreateClient(apiKey, secretKey);

        var end = DateTime.UtcNow.Date;
        var start = end.AddDays(-limit - 5);

        var url = $"{DataBaseUrl}/v2/stocks/{symbol}/bars?timeframe={timeframe}&start={start:yyyy-MM-dd}&end={end:yyyy-MM-dd}&limit={limit}&adjustment=split&feed=iex";
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var bars = new List<AlpacaBarDto>();
        if (doc.RootElement.TryGetProperty("bars", out var barsElement) && barsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var bar in barsElement.EnumerateArray())
            {
                var timestamp = bar.GetProperty("t").GetString()!;
                var time = new DateTimeOffset(DateTime.Parse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)).ToUnixTimeMilliseconds();
                var close = decimal.Parse(bar.GetProperty("c").GetRawText(), CultureInfo.InvariantCulture);
                bars.Add(new AlpacaBarDto(time, close));
            }
        }

        return bars;
    }

    public async Task<List<AlpacaBarDto>> GetBarsForUserAsync(Guid userId, string symbol, string timeframe = "1Day", int limit = 30)
    {
        var account = await alpacaRepo.GetByUserIdAsync(userId);
        if (account == null || !account.IsConnected)
            return [];

        return await GetBarsAsync(symbol, account.ApiKey, account.SecretKey, timeframe, limit);
    }

    private static async Task<AlpacaAccountDataDto> FetchAlpacaAccountData(string apiKey, string secretKey, bool isPaper)
    {
        var baseUrl = isPaper ? PaperBaseUrl : LiveBaseUrl;
        using var client = CreateClient(apiKey, secretKey);

        var accountJson = await client.GetStringAsync($"{baseUrl}/v2/account");
        using var accountDoc = JsonDocument.Parse(accountJson);
        var acc = accountDoc.RootElement;

        var equity = ParseDecimal(acc, "equity");
        var cash = ParseDecimal(acc, "cash");
        var buyingPower = ParseDecimal(acc, "buying_power");
        var longMarketValue = ParseDecimal(acc, "long_market_value");
        var status = acc.GetProperty("status").GetString() ?? "UNKNOWN";

        var positionsJson = await client.GetStringAsync($"{baseUrl}/v2/positions");
        using var posDoc = JsonDocument.Parse(positionsJson);

        var positions = new List<AlpacaPositionDto>();
        foreach (var pos in posDoc.RootElement.EnumerateArray())
        {
            positions.Add(new AlpacaPositionDto(
                pos.GetProperty("symbol").GetString()!,
                ParseDecimal(pos, "qty"),
                ParseDecimal(pos, "avg_entry_price"),
                ParseDecimal(pos, "current_price"),
                ParseDecimal(pos, "market_value"),
                ParseDecimal(pos, "unrealized_pl"),
                ParseDecimal(pos, "unrealized_plpc"),
                ParseDecimal(pos, "change_today")
            ));
        }

        positions = positions.OrderByDescending(p => p.MarketValue).ToList();

        return new AlpacaAccountDataDto(true, DateTime.UtcNow, equity, cash, buyingPower, longMarketValue, status, isPaper, positions);
    }

    private static HttpClient CreateClient(string apiKey, string secretKey)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("APCA-API-KEY-ID", apiKey);
        client.DefaultRequestHeaders.Add("APCA-API-SECRET-KEY", secretKey);
        return client;
    }

    private static decimal ParseDecimal(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var prop)) return 0;
        var raw = prop.GetString() ?? prop.GetRawText();
        return decimal.TryParse(raw, CultureInfo.InvariantCulture, out var val) ? val : 0;
    }
}
