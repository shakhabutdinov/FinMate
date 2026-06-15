using System.Globalization;
using System.Text.Json;
using aspnet.DTOs;
using aspnet.Models;
using aspnet.Repositories.Interfaces;

namespace aspnet.Services;

public class StockService(
    IStockHoldingRepository stockRepo,
    ITrendingItemRepository trendingRepo,
    IAlpacaAccountRepository alpacaRepo,
    HttpClient httpClient)
{

    private const string AlpacaDataUrl = "https://data.alpaca.markets/v2/stocks/quotes/latest";

    public async Task<StockPortfolioDto> GetPortfolioAsync(Guid userId)
    {
        var holdings = await stockRepo.GetByUserIdAsync(userId);
        var trending = await trendingRepo.GetByCategoryAsync("stock");


        var alpacaAccount = await alpacaRepo.GetByUserIdAsync(userId);
        string? apiKey = alpacaAccount?.IsConnected == true ? alpacaAccount.ApiKey : null;
        string? secretKey = alpacaAccount?.IsConnected == true ? alpacaAccount.SecretKey : null;

        var symbols = holdings.Select(h => h.Symbol).Distinct().ToList();
        var livePrices = await FetchLivePricesAsync(symbols, apiKey, secretKey);

        decimal totalBalance = 0;
        decimal totalDayChange = 0;
        var holdingDtos = new List<StockHoldingDto>();

        foreach (var h in holdings)
        {
            var costBasis = h.PricePerShare; 

            if (livePrices.TryGetValue(h.Symbol, out var livePrice))
                h.PricePerShare = livePrice;

            var value = h.Quantity * h.PricePerShare;
            totalBalance += value;

            holdingDtos.Add(new StockHoldingDto(
                h.Id, h.Symbol, h.CompanyName,
                h.PricePerShare, h.Quantity, value, h.Color,
                costBasis)); 
        }


        var trendingDtos = trending.Select(t => new TrendingStockDto(
            t.Symbol, t.Price, t.ChangePercent, t.ChartData
        )).ToList();


        foreach (var h in holdings)
        {
            var trend = trendingDtos.FirstOrDefault(t => t.Symbol == h.Symbol);
            if (trend is not null)
            {
                var prev = h.PricePerShare / (1 + trend.ChangePercent / 100m);
                totalDayChange += (h.PricePerShare - prev) * h.Quantity;
            }
        }

        var overallChangePercent = totalBalance > 0
            ? Math.Round(totalDayChange / totalBalance * 100, 2)
            : 0;

        return new StockPortfolioDto(
            totalBalance,
            totalDayChange,
            overallChangePercent,
            holdingDtos,
            trendingDtos);
    }

    public async Task<StockHoldingDto> CreateHoldingAsync(Guid userId, CreateStockHoldingDto dto)
    {
        var holding = new StockHolding
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Symbol = dto.Symbol,
            CompanyName = dto.CompanyName,
            PricePerShare = dto.PricePerShare,
            Quantity = dto.Quantity,
            Color = dto.Color
        };

        await stockRepo.CreateAsync(holding);
        return new StockHoldingDto(
            holding.Id, holding.Symbol, holding.CompanyName,
            holding.PricePerShare, holding.Quantity, holding.TotalValue, holding.Color);
    }

    public async Task DeleteHoldingAsync(Guid id) =>
        await stockRepo.DeleteAsync(id);


    private async Task<Dictionary<string, decimal>> FetchLivePricesAsync(
        List<string> symbols,
        string? apiKey,
        string? secretKey)
    {
        var result = new Dictionary<string, decimal>();
        if (symbols.Count == 0) return result;

        try
        {
            var symbolsParam = string.Join(",", symbols);
            var url = $"{AlpacaDataUrl}?symbols={symbolsParam}&feed=iex";

            if (apiKey is not null && secretKey is not null)
            {
                httpClient.DefaultRequestHeaders.Remove("APCA-API-KEY-ID");
                httpClient.DefaultRequestHeaders.Remove("APCA-API-SECRET-KEY");
                httpClient.DefaultRequestHeaders.Add("APCA-API-KEY-ID", apiKey);
                httpClient.DefaultRequestHeaders.Add("APCA-API-SECRET-KEY", secretKey);
            }

            var json = await httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("quotes", out var quotes))
                return result;

            foreach (var sym in symbols)
            {
                if (!quotes.TryGetProperty(sym, out var quote)) continue;


                if (quote.TryGetProperty("ap", out var ap) &&
                    decimal.TryParse(ap.GetRawText(), CultureInfo.InvariantCulture, out var price)
                    && price > 0)
                {
                    result[sym] = price;
                }
            }
        }
        catch
        {

        }

        return result;
    }
}