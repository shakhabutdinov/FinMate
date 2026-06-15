using System.Globalization;
using System.Text;
using System.Text.Json;
using aspnet.DTOs;
using aspnet.Models;
using aspnet.Repositories.Interfaces;

namespace aspnet.Services;

public class AlpacaService(IAlpacaAccountRepository alpacaRepo)
{
    private const string LiveBaseUrl  = "https://api.alpaca.markets";
    private const string PaperBaseUrl = "https://paper-api.alpaca.markets";
    private const string DataBaseUrl  = "https://data.alpaca.markets";



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
            existing.ApiKey      = dto.ApiKey;
            existing.SecretKey   = dto.SecretKey;
            existing.IsPaper     = dto.IsPaper;
            existing.IsConnected = true;
            existing.ConnectedAt = DateTime.UtcNow;
            await alpacaRepo.UpdateAsync(existing);
        }
        else
        {
            await alpacaRepo.CreateAsync(new AlpacaAccount
            {
                Id           = Guid.NewGuid(),
                UserId       = userId,
                ApiKey       = dto.ApiKey,
                SecretKey    = dto.SecretKey,
                IsPaper      = dto.IsPaper,
                IsConnected  = true,
                ConnectedAt  = DateTime.UtcNow
            });
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



    public async Task<List<AlpacaBarDto>> GetBarsAsync(
        string symbol, string apiKey, string secretKey,
        string timeframe = "1Day", int limit = 30)
    {
        using var client = CreateClient(apiKey, secretKey);

        var end   = DateTime.UtcNow.Date;
        var start = end.AddDays(-limit - 5);

        var url = $"{DataBaseUrl}/v2/stocks/{symbol}/bars" +
                  $"?timeframe={timeframe}&start={start:yyyy-MM-dd}&end={end:yyyy-MM-dd}" +
                  $"&limit={limit}&adjustment=split&feed=iex";

        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json      = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var bars = new List<AlpacaBarDto>();
        if (doc.RootElement.TryGetProperty("bars", out var barsEl) &&
            barsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var bar in barsEl.EnumerateArray())
            {
                var ts    = bar.GetProperty("t").GetString()!;
                var time  = new DateTimeOffset(
                    DateTime.Parse(ts, CultureInfo.InvariantCulture,
                                   System.Globalization.DateTimeStyles.RoundtripKind))
                    .ToUnixTimeMilliseconds();
                var close = decimal.Parse(
                    bar.GetProperty("c").GetRawText(), CultureInfo.InvariantCulture);
                bars.Add(new AlpacaBarDto(time, close));
            }
        }

        return bars;
    }

    public async Task<List<AlpacaBarDto>> GetBarsForUserAsync(
        Guid userId, string symbol, string timeframe = "1Day", int limit = 30)
    {
        var account = await alpacaRepo.GetByUserIdAsync(userId);
        if (account == null || !account.IsConnected) return [];
        return await GetBarsAsync(symbol, account.ApiKey, account.SecretKey, timeframe, limit);
    }

    public async Task<OrderResultDto> PlaceOrderAsync(Guid userId, PlaceOrderDto dto)
    {
        var account = await alpacaRepo.GetByUserIdAsync(userId)
            ?? throw new InvalidOperationException("Alpaca account not connected.");

        if (!account.IsConnected)
            throw new InvalidOperationException("Alpaca account is not connected.");

        var side = dto.Side.ToLower();
        var type = dto.Type.ToLower();

        if (side is not ("buy" or "sell"))
            throw new ArgumentException("Order side must be 'buy' or 'sell'.");

        if (type is not ("market" or "limit"))
            throw new ArgumentException("Order type must be 'market' or 'limit'.");

        if (type == "limit" && dto.LimitPrice is null)
            throw new ArgumentException("A limit price is required for limit orders.");


        var orderBody = new Dictionary<string, object>
        {
            ["symbol"]        = dto.Symbol.ToUpper(),
            ["qty"]           = dto.Qty.ToString(CultureInfo.InvariantCulture),
            ["side"]          = side,
            ["type"]          = type,
            ["time_in_force"] = dto.TimeInForce.ToLower()
        };

        if (type == "limit")
            orderBody["limit_price"] = dto.LimitPrice!.Value.ToString(CultureInfo.InvariantCulture);

        var baseUrl  = account.IsPaper ? PaperBaseUrl : LiveBaseUrl;
        using var client = CreateClient(account.ApiKey, account.SecretKey);

        var json     = JsonSerializer.Serialize(orderBody);
        var content  = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"{baseUrl}/v2/orders", content);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Alpaca rejected the order ({response.StatusCode}): {errorBody}");
        }

        var resultJson = await response.Content.ReadAsStringAsync();
        using var doc  = JsonDocument.Parse(resultJson);
        var r          = doc.RootElement;

        return new OrderResultDto(
            OrderId:       r.GetProperty("id").GetString()!,
            Symbol:        r.GetProperty("symbol").GetString()!,
            Qty:           ParseDecimal(r, "qty"),
            Side:          r.GetProperty("side").GetString()!,
            Type:          r.GetProperty("type").GetString()!,
            Status:        r.GetProperty("status").GetString()!,
            FilledAvgPrice: TryParseDecimal(r, "filled_avg_price"),
            LimitPrice:    TryParseDecimal(r, "limit_price"),
            TimeInForce:   r.GetProperty("time_in_force").GetString()!,
            SubmittedAt:   DateTime.Parse(
                r.GetProperty("submitted_at").GetString()!,
                CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind)
        );
    }


    public async Task<List<OrderSummaryDto>> GetOrdersAsync(Guid userId, int limit = 20)
    {
        var account = await alpacaRepo.GetByUserIdAsync(userId);
        if (account == null || !account.IsConnected) return [];

        var baseUrl      = account.IsPaper ? PaperBaseUrl : LiveBaseUrl;
        using var client = CreateClient(account.ApiKey, account.SecretKey);

        var response = await client.GetAsync(
            $"{baseUrl}/v2/orders?status=all&limit={limit}&direction=desc");

        response.EnsureSuccessStatusCode();

        var json      = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var orders = new List<OrderSummaryDto>();
        foreach (var o in doc.RootElement.EnumerateArray())
        {
            orders.Add(new OrderSummaryDto(
                OrderId:       o.GetProperty("id").GetString()!,
                Symbol:        o.GetProperty("symbol").GetString()!,
                Qty:           ParseDecimal(o, "qty"),
                FilledQty:     ParseDecimal(o, "filled_qty"),
                Side:          o.GetProperty("side").GetString()!,
                Type:          o.GetProperty("type").GetString()!,
                Status:        o.GetProperty("status").GetString()!,
                FilledAvgPrice: TryParseDecimal(o, "filled_avg_price"),
                SubmittedAt:   DateTime.Parse(
                    o.GetProperty("submitted_at").GetString()!,
                    CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind)
            ));
        }

        return orders;
    }


    public async Task CancelOrderAsync(Guid userId, string orderId)
    {
        var account = await alpacaRepo.GetByUserIdAsync(userId)
            ?? throw new InvalidOperationException("Alpaca account not connected.");

        var baseUrl      = account.IsPaper ? PaperBaseUrl : LiveBaseUrl;
        using var client = CreateClient(account.ApiKey, account.SecretKey);

        var response = await client.DeleteAsync($"{baseUrl}/v2/orders/{orderId}");


        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.UnprocessableEntity)
            response.EnsureSuccessStatusCode();
    }

 

    private static async Task<AlpacaAccountDataDto> FetchAlpacaAccountData(
        string apiKey, string secretKey, bool isPaper)
    {
        var baseUrl = isPaper ? PaperBaseUrl : LiveBaseUrl;
        using var client = CreateClient(apiKey, secretKey);

        var accountJson = await client.GetStringAsync($"{baseUrl}/v2/account");
        using var accountDoc = JsonDocument.Parse(accountJson);
        var acc = accountDoc.RootElement;

        var equity          = ParseDecimal(acc, "equity");
        var cash            = ParseDecimal(acc, "cash");
        var buyingPower     = ParseDecimal(acc, "buying_power");
        var longMarketValue = ParseDecimal(acc, "long_market_value");
        var status          = acc.GetProperty("status").GetString() ?? "UNKNOWN";

        var positionsJson = await client.GetStringAsync($"{baseUrl}/v2/positions");
        using var posDoc  = JsonDocument.Parse(positionsJson);

        var positions = posDoc.RootElement.EnumerateArray().Select(pos =>
            new AlpacaPositionDto(
                pos.GetProperty("symbol").GetString()!,
                ParseDecimal(pos, "qty"),
                ParseDecimal(pos, "avg_entry_price"),
                ParseDecimal(pos, "current_price"),
                ParseDecimal(pos, "market_value"),
                ParseDecimal(pos, "unrealized_pl"),
                ParseDecimal(pos, "unrealized_plpc"),
                ParseDecimal(pos, "change_today")
            )).OrderByDescending(p => p.MarketValue).ToList();

        return new AlpacaAccountDataDto(
            true, DateTime.UtcNow,
            equity, cash, buyingPower, longMarketValue,
            status, isPaper, positions);
    }

    private static HttpClient CreateClient(string apiKey, string secretKey)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("APCA-API-KEY-ID", apiKey);
        client.DefaultRequestHeaders.Add("APCA-API-SECRET-KEY", secretKey);
        return client;
    }

    private static decimal ParseDecimal(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var p)) return 0;
        var raw = p.ValueKind == JsonValueKind.String ? p.GetString()! : p.GetRawText();
        return decimal.TryParse(raw, CultureInfo.InvariantCulture, out var val) ? val : 0;
    }

    private static decimal? TryParseDecimal(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var p)) return null;
        if (p.ValueKind == JsonValueKind.Null) return null;
        var raw = p.ValueKind == JsonValueKind.String ? p.GetString()! : p.GetRawText();
        return decimal.TryParse(raw, CultureInfo.InvariantCulture, out var val) ? val : null;
    }
}