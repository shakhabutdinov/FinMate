using aspnet.DTOs;
using aspnet.Models;
using aspnet.Repositories.Interfaces;

namespace aspnet.Services;

public class CryptoService(ICryptoHoldingRepository cryptoRepo, ITrendingItemRepository trendingRepo)
{
    public async Task<CryptoPortfolioDto> GetPortfolioAsync(Guid userId)
    {
        var holdings = await cryptoRepo.GetByUserIdAsync(userId);
        var trending = await trendingRepo.GetByCategoryAsync("crypto");

        var totalBalance = holdings.Sum(h => h.TotalValue);

        var holdingDtos = holdings.Select(h => new CryptoHoldingDto(
            h.Id, h.Symbol, h.Name, h.PricePerUnit, h.Amount, h.TotalValue, h.Color
        )).ToList();

        var trendingDtos = trending.Select(t => new TrendingCryptoDto(
            t.Symbol, t.Price, t.ChangePercent, t.ChartData
        )).ToList();

        return new CryptoPortfolioDto(totalBalance, 1240.50m, 2.4m, holdingDtos, trendingDtos);
    }

    public async Task<CryptoHoldingDto> CreateHoldingAsync(Guid userId, CreateCryptoHoldingDto dto)
    {
        var holding = new CryptoHolding
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Symbol = dto.Symbol,
            Name = dto.Name,
            PricePerUnit = dto.PricePerUnit,
            Amount = dto.Amount,
            Color = dto.Color
        };

        await cryptoRepo.CreateAsync(holding);
        return new CryptoHoldingDto(holding.Id, holding.Symbol, holding.Name, holding.PricePerUnit, holding.Amount, holding.TotalValue, holding.Color);
    }

    public async Task DeleteHoldingAsync(Guid id) =>
        await cryptoRepo.DeleteAsync(id);
}
