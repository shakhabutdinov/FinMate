using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using aspnet.DTOs;
using aspnet.Models;
using aspnet.Repositories.Interfaces;

namespace aspnet.Services;

public class BinanceService(IBinanceAccountRepository binanceRepo)
{
    private const string BinanceBaseUrl = "https://api.binance.com";

    public async Task<BinanceStatusDto> GetStatusAsync(Guid userId)
    {
        var account = await binanceRepo.GetByUserIdAsync(userId);
        if (account == null)
            return new BinanceStatusDto(false, null);

        return new BinanceStatusDto(account.IsConnected, account.ConnectedAt);
    }

    public async Task<BinanceAccountDataDto> ConnectAsync(Guid userId, ConnectBinanceDto dto)
    {
        var accountData = await FetchBinanceAccountData(dto.ApiKey, dto.SecretKey);

        var existing = await binanceRepo.GetByUserIdAsync(userId);
        if (existing != null)
        {
            existing.ApiKey = dto.ApiKey;
            existing.SecretKey = dto.SecretKey;
            existing.IsConnected = true;
            existing.ConnectedAt = DateTime.UtcNow;
            await binanceRepo.UpdateAsync(existing);
        }
        else
        {
            var account = new BinanceAccount
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ApiKey = dto.ApiKey,
                SecretKey = dto.SecretKey,
                IsConnected = true,
                ConnectedAt = DateTime.UtcNow
            };
            await binanceRepo.CreateAsync(account);
        }

        return accountData;
    }

    public async Task<BinanceAccountDataDto> GetAccountDataAsync(Guid userId)
    {
        var account = await binanceRepo.GetByUserIdAsync(userId);
        if (account == null || !account.IsConnected)
            return new BinanceAccountDataDto(false, null, 0, []);

        try
        {
            return await FetchBinanceAccountData(account.ApiKey, account.SecretKey);
        }
        catch
        {
            return new BinanceAccountDataDto(true, account.ConnectedAt, 0, []);
        }
    }

    public async Task DisconnectAsync(Guid userId) =>
        await binanceRepo.DeleteAsync(userId);

    public static async Task<List<KlinePointDto>> GetKlinesAsync(string symbol, string interval = "1d", int limit = 30)
    {
        using var client = new HttpClient();
        var pair = symbol is "USDT" or "BUSD" or "FDUSD" ? "BTCUSDT" : $"{symbol}USDT";
        var url = $"{BinanceBaseUrl}/api/v3/klines?symbol={pair}&interval={interval}&limit={limit}";
        var json = await client.GetStringAsync(url);
        using var doc = JsonDocument.Parse(json);

        return doc.RootElement.EnumerateArray().Select(k => new KlinePointDto(
            k[0].GetInt64(),
            decimal.Parse(k[4].GetString()!, CultureInfo.InvariantCulture)
        )).ToList();
    }

    private static async Task<BinanceAccountDataDto> FetchBinanceAccountData(string apiKey, string secretKey)
    {
        using var client = new HttpClient();

        var json = await SignedGetAsync(client, "/api/v3/account", apiKey, secretKey);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var prices = await FetchAllPrices();

        var assets = new List<BinanceAssetDto>();
        decimal totalUsdt = 0;

        foreach (var balance in root.GetProperty("balances").EnumerateArray())
        {
            var asset = balance.GetProperty("asset").GetString()!;
            var free = decimal.Parse(balance.GetProperty("free").GetString()!, CultureInfo.InvariantCulture);
            var locked = decimal.Parse(balance.GetProperty("locked").GetString()!, CultureInfo.InvariantCulture);
            var total = free + locked;

            if (total <= 0) continue;

            decimal usdtValue = 0;
            if (asset is "USDT" or "BUSD" or "FDUSD")
                usdtValue = total;
            else if (prices.TryGetValue($"{asset}USDT", out var price))
                usdtValue = total * price;

            if (usdtValue < 0.01m) continue;

            totalUsdt += usdtValue;
            assets.Add(new BinanceAssetDto(asset, free, locked, total, usdtValue));
        }

        assets = assets.OrderByDescending(a => a.UsdtValue).ToList();

        return new BinanceAccountDataDto(true, DateTime.UtcNow, totalUsdt, assets);
    }

    private static async Task<string> SignedGetAsync(HttpClient client, string endpoint, string apiKey, string secret)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var query = $"timestamp={timestamp}";
        var signature = Sign(query, secret);

        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("X-MBX-APIKEY", apiKey);

        var url = $"{BinanceBaseUrl}{endpoint}?{query}&signature={signature}";
        return await client.GetStringAsync(url);
    }

    private static async Task<Dictionary<string, decimal>> FetchAllPrices()
    {
        using var client = new HttpClient();
        var json = await client.GetStringAsync($"{BinanceBaseUrl}/api/v3/ticker/price");
        using var doc = JsonDocument.Parse(json);

        var prices = new Dictionary<string, decimal>();
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var symbol = item.GetProperty("symbol").GetString()!;
            if (decimal.TryParse(item.GetProperty("price").GetString(), CultureInfo.InvariantCulture, out var price))
                prices[symbol] = price;
        }
        return prices;
    }

    private static string Sign(string data, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data))).ToLower();
    }
}
