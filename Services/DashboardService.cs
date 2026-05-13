using aspnet.DTOs;
using aspnet.Models;
using aspnet.Repositories.Interfaces;

namespace aspnet.Services;

public class DashboardService(
    IAssetRepository assetRepository,
    IAssetSnapshotRepository snapshotRepository,
    IStockHoldingRepository stockHoldingRepo,
    ICryptoHoldingRepository cryptoHoldingRepo,
    AlpacaService alpacaService)
{
    public async Task<DashboardDto> GetDashboardAsync(Guid userId)
    {
        // 1. Manual assets (bank accounts, savings, etc.) 
        var assets    = await assetRepository.GetByUserIdAsync(userId);
        var assetDtos = new List<AssetDto>();

        foreach (var asset in assets)
        {
            await RecordTodaySnapshot(asset);

            var snapshots    = await snapshotRepository.GetByAssetIdAsync(asset.Id, 7);
            var sparkline    = snapshots.Select(s => s.Balance).ToList();
            if (sparkline.Count == 0) sparkline = [asset.Balance];

            var changePercent = ComputeChangePercent(sparkline, asset.Balance);
            asset.ChangePercent = changePercent;
            await assetRepository.UpdateAsync(asset);

            assetDtos.Add(new AssetDto(
                asset.Id, asset.Name, asset.Type.ToString(),
                asset.Balance, changePercent, asset.Icon, sparkline));
        }

        // 2. Stock holdings 
        var stockHoldings = await stockHoldingRepo.GetByUserIdAsync(userId);
        var stockTotal    = stockHoldings.Sum(s => s.TotalValue);
        if (stockTotal > 0)
        {
            assetDtos.Add(new AssetDto(
                Guid.Empty, "Stock Portfolio", "Investment",
                stockTotal, 0, "chart-line",
                [stockTotal]));
        }

        // 3. Alpaca live brokerage 
        var alpacaAccount = await alpacaService.GetAccountDataAsync(userId);
        if (alpacaAccount.IsConnected && alpacaAccount.Equity > 0)
        {
            // Day change % from positions average
            var alpacaChange = alpacaAccount.Positions.Count > 0
                ? Math.Round(alpacaAccount.Positions.Average(p => p.ChangeToday * 100), 2)
                : 0m;

            var label = alpacaAccount.IsPaper ? "Alpaca (Paper)" : "Alpaca Brokerage";
            assetDtos.Add(new AssetDto(
                Guid.Empty, label, "Investment",
                alpacaAccount.Equity, alpacaChange, "trending-up",
                [alpacaAccount.Equity]));
        }

        // 4. Crypto holdings 
        var cryptoHoldings = await cryptoHoldingRepo.GetByUserIdAsync(userId);
        var cryptoTotal    = cryptoHoldings.Sum(c => c.TotalValue);
        if (cryptoTotal > 0)
        {
            assetDtos.Add(new AssetDto(
                Guid.Empty, "Crypto Portfolio", "Investment",
                cryptoTotal, 0, "bitcoin",
                [cryptoTotal]));
        }

        // 5. Grand total 
        var totalBalance   = assetDtos.Sum(a => a.Balance);
        var overallChange  = assetDtos.Count > 0
            ? assetDtos.Average(a => a.ChangePercent)
            : 0m;
        var changeAmount   = totalBalance * overallChange / 100m;

        var quickActions = new List<QuickActionDto>
        {
            new("Income Statement", "file-text"),
            new("Cash Flow",        "file-text"),
            new("Balance Sheet",    "file-text")
        };

        return new DashboardDto(totalBalance, overallChange, changeAmount, assetDtos, quickActions);
    }

    private async Task RecordTodaySnapshot(Asset asset)
    {
        if (await snapshotRepository.ExistsForTodayAsync(asset.Id)) return;

        await snapshotRepository.CreateAsync(new AssetSnapshot
        {
            Id      = Guid.NewGuid(),
            AssetId = asset.Id,
            Date    = DateTime.UtcNow.Date,
            Balance = asset.Balance
        });
    }

    private static decimal ComputeChangePercent(List<decimal> sparkline, decimal currentBalance)
    {
        if (sparkline.Count < 2) return 0;
        var yesterday = sparkline[^2];
        if (yesterday == 0) return 0;
        return Math.Round((currentBalance - yesterday) / yesterday * 100m, 2);
    }
}