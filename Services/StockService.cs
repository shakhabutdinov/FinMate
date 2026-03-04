using aspnet.DTOs;
using aspnet.Models;
using aspnet.Repositories.Interfaces;

namespace aspnet.Services;

public class StockService(IStockHoldingRepository stockRepo, ITrendingItemRepository trendingRepo)
{
    public async Task<StockPortfolioDto> GetPortfolioAsync(Guid userId)
    {
        var holdings = await stockRepo.GetByUserIdAsync(userId);
        var trending = await trendingRepo.GetByCategoryAsync("stock");

        var totalBalance = holdings.Sum(h => h.TotalValue);

        var holdingDtos = holdings.Select(h => new StockHoldingDto(
            h.Id, h.Symbol, h.CompanyName, h.PricePerShare, h.Quantity, h.TotalValue, h.Color
        )).ToList();

        var trendingDtos = trending.Select(t => new TrendingStockDto(
            t.Symbol, t.Price, t.ChangePercent, t.ChartData
        )).ToList();

        return new StockPortfolioDto(totalBalance, 345.50m, 1.4m, holdingDtos, trendingDtos);
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
        return new StockHoldingDto(holding.Id, holding.Symbol, holding.CompanyName, holding.PricePerShare, holding.Quantity, holding.TotalValue, holding.Color);
    }

    public async Task DeleteHoldingAsync(Guid id) =>
        await stockRepo.DeleteAsync(id);
}
