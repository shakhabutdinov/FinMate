using aspnet.DTOs;
using aspnet.Models;
using aspnet.Repositories.Interfaces;

namespace aspnet.Services;

public class DashboardService(IAssetRepository assetRepository, IAssetSnapshotRepository snapshotRepository)
{
    public async Task<DashboardDto> GetDashboardAsync(Guid userId)
    {
        var assets = await assetRepository.GetByUserIdAsync(userId);
        var assetDtos = new List<AssetDto>();

        foreach (var asset in assets)
        {
            await RecordTodaySnapshot(asset);

            var snapshots = await snapshotRepository.GetByAssetIdAsync(asset.Id, 7);
            var sparkline = snapshots.Select(s => s.Balance).ToList();

            if (sparkline.Count == 0)
                sparkline = [asset.Balance];

            var changePercent = ComputeChangePercent(sparkline, asset.Balance);

            asset.ChangePercent = changePercent;
            await assetRepository.UpdateAsync(asset);

            assetDtos.Add(new AssetDto(
                asset.Id, asset.Name, asset.Type.ToString(), asset.Balance, changePercent, asset.Icon, sparkline
            ));
        }

        var totalBalance = assets.Sum(a => a.Balance);
        var changePercent2 = assetDtos.Count > 0
            ? assetDtos.Average(a => a.ChangePercent)
            : 0;
        var changeAmount = totalBalance * changePercent2 / 100m;

        var quickActions = new List<QuickActionDto>
        {
            new("Income Statement", "file-text"),
            new("Cash Flow", "file-text"),
            new("Balance Sheet", "file-text")
        };

        return new DashboardDto(totalBalance, changePercent2, changeAmount, assetDtos, quickActions);
    }

    private async Task RecordTodaySnapshot(Asset asset)
    {
        if (await snapshotRepository.ExistsForTodayAsync(asset.Id))
            return;

        await snapshotRepository.CreateAsync(new AssetSnapshot
        {
            Id = Guid.NewGuid(),
            AssetId = asset.Id,
            Date = DateTime.UtcNow.Date,
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
