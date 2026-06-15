using System.Globalization;
using System.Text.Json;
using aspnet.DTOs;
using aspnet.Models;
using aspnet.Repositories.Interfaces;

namespace aspnet.Services;

public class CryptoService(
    ICryptoHoldingRepository cryptoRepo,
    ITrendingItemRepository trendingRepo,
    HttpClient httpClient)
{
    private const string BinanceTickerUrl = "https://api.binance.com/api/v3/ticker/24hr";

    public async Task<CryptoPortfolioDto> GetPortfolioAsync(Guid userId)
    {
        var holdings = await cryptoRepo.GetByUserIdAsync(userId);
        var trending = await trendingRepo.GetByCategoryAsync("crypto");


        var livePrices = await FetchLivePricesAsync(
            holdings.Select(h => h.Symbol).Distinct().ToList());

        decimal totalBalance = 0;
        decimal totalDayChange = 0;
        var holdingDtos = new List<CryptoHoldingDto>();

        foreach (var h in holdings)
        {
            var costBasis = h.PricePerUnit; 

            if (livePrices.TryGetValue(h.Symbol, out var live))
                h.PricePerUnit = live.Price;

            var value = h.Amount * h.PricePerUnit;
            var dayChange = livePrices.TryGetValue(h.Symbol, out var liveData)
                ? value * liveData.ChangePercent / 100m
                : 0;

            totalBalance += value;
            totalDayChange += dayChange;

            holdingDtos.Add(new CryptoHoldingDto(
                h.Id, h.Symbol, h.Name, h.PricePerUnit,
                h.Amount, value, h.Color, costBasis)); 
        }


        var trendingDtos = trending.Select(t =>
        {
            var changePercent = livePrices.TryGetValue(t.Symbol, out var lp)
                ? lp.ChangePercent
                : t.ChangePercent;

            var price = livePrices.TryGetValue(t.Symbol, out var lp2)
                ? lp2.Price
                : t.Price;

            return new TrendingCryptoDto(t.Symbol, price, changePercent, t.ChartData);
        }).ToList();

        var overallChangePercent = totalBalance > 0
            ? Math.Round(totalDayChange / totalBalance * 100, 2)
            : 0;

        return new CryptoPortfolioDto(
            totalBalance,
            totalDayChange,
            overallChangePercent,
            holdingDtos,
            trendingDtos);
    }

    public async Task<CryptoHoldingDto> CreateHoldingAsync(Guid userId, CreateCryptoHoldingDto dto)
    {

        var livePrice = await GetLivePriceAsync(dto.Symbol);
        var priceToUse = livePrice > 0 ? livePrice : dto.PricePerUnit;

        var holding = new CryptoHolding
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Symbol = dto.Symbol.ToUpper(),
            Name = dto.Name,
            PricePerUnit = priceToUse,
            Amount = dto.Amount,
            Color = dto.Color
        };

        await cryptoRepo.CreateAsync(holding);
        return new CryptoHoldingDto(
            holding.Id, holding.Symbol, holding.Name,
            holding.PricePerUnit, holding.Amount, holding.TotalValue, holding.Color);
    }

    public async Task DeleteHoldingAsync(Guid id) =>
        await cryptoRepo.DeleteAsync(id);


    public async Task<decimal> GetLivePriceAsync(string symbol)
    {
        try
        {
            var url = $"https://api.binance.com/api/v3/ticker/price?symbol={symbol.ToUpper()}USDT";
            var json = await httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            var priceStr = doc.RootElement.GetProperty("price").GetString()!;
            return decimal.Parse(priceStr, CultureInfo.InvariantCulture);
        }
        catch
        {
            return 0;
        }
    }



    private async Task<Dictionary<string, (decimal Price, decimal ChangePercent)>> FetchLivePricesAsync(
        List<string> symbols)
    {
        var result = new Dictionary<string, (decimal, decimal)>();

        try
        {
            var json = await httpClient.GetStringAsync(BinanceTickerUrl);
            using var doc = JsonDocument.Parse(json);

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var tickerSymbol = item.GetProperty("symbol").GetString() ?? "";

                foreach (var sym in symbols)
                {

                    if (!tickerSymbol.Equals($"{sym}USDT", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var price = decimal.Parse(
                        item.GetProperty("lastPrice").GetString()!,
                        CultureInfo.InvariantCulture);

                    var changePercent = decimal.Parse(
                        item.GetProperty("priceChangePercent").GetString()!,
                        CultureInfo.InvariantCulture);

                    result[sym] = (price, changePercent);
                }
            }
        }
        catch
        {

        }

        return result;
    }
}