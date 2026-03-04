using Microsoft.EntityFrameworkCore;
using aspnet.Models;

namespace aspnet.Data;

public class FinMateDbContext(DbContextOptions<FinMateDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<StockHolding> StockHoldings => Set<StockHolding>();
    public DbSet<CryptoHolding> CryptoHoldings => Set<CryptoHolding>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<FinancialGoal> FinancialGoals => Set<FinancialGoal>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<TrendingItem> TrendingItems => Set<TrendingItem>();
    public DbSet<BinanceAccount> BinanceAccounts => Set<BinanceAccount>();
    public DbSet<AlpacaAccount> AlpacaAccounts => Set<AlpacaAccount>();
    public DbSet<AssetSnapshot> AssetSnapshots => Set<AssetSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.HasIndex(u => u.GoogleId).IsUnique().HasFilter("\"GoogleId\" IS NOT NULL");
        });

        modelBuilder.Entity<Asset>(e =>
        {
            e.HasOne(a => a.User).WithMany(u => u.Assets).HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
            e.Property(a => a.Balance).HasPrecision(18, 2);
            e.Property(a => a.ChangePercent).HasPrecision(10, 2);
        });

        modelBuilder.Entity<StockHolding>(e =>
        {
            e.HasOne(s => s.User).WithMany(u => u.StockHoldings).HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
            e.Property(s => s.PricePerShare).HasPrecision(18, 2);
        });

        modelBuilder.Entity<CryptoHolding>(e =>
        {
            e.HasOne(c => c.User).WithMany(u => u.CryptoHoldings).HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
            e.Property(c => c.PricePerUnit).HasPrecision(18, 8);
            e.Property(c => c.Amount).HasPrecision(18, 8);
        });

        modelBuilder.Entity<Transaction>(e =>
        {
            e.HasOne(t => t.User).WithMany(u => u.Transactions).HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
            e.Property(t => t.Amount).HasPrecision(18, 2);
            e.HasIndex(t => new { t.UserId, t.Date });
        });

        modelBuilder.Entity<FinancialGoal>(e =>
        {
            e.HasOne(g => g.User).WithMany(u => u.FinancialGoals).HasForeignKey(g => g.UserId).OnDelete(DeleteBehavior.Cascade);
            e.Property(g => g.TargetAmount).HasPrecision(18, 2);
            e.Property(g => g.CurrentAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ChatMessage>(e =>
        {
            e.HasOne(m => m.User).WithMany(u => u.ChatMessages).HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(m => new { m.UserId, m.CreatedAt });
        });

        modelBuilder.Entity<TrendingItem>(e =>
        {
            e.Property(t => t.Price).HasPrecision(18, 8);
            e.Property(t => t.ChangePercent).HasPrecision(10, 2);
        });

        modelBuilder.Entity<BinanceAccount>(e =>
        {
            e.HasOne(b => b.User).WithOne(u => u.BinanceAccount).HasForeignKey<BinanceAccount>(b => b.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(b => b.UserId).IsUnique();
        });

        modelBuilder.Entity<AlpacaAccount>(e =>
        {
            e.HasOne(a => a.User).WithOne(u => u.AlpacaAccount).HasForeignKey<AlpacaAccount>(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(a => a.UserId).IsUnique();
        });

        modelBuilder.Entity<AssetSnapshot>(e =>
        {
            e.HasOne(s => s.Asset).WithMany(a => a.Snapshots).HasForeignKey(s => s.AssetId).OnDelete(DeleteBehavior.Cascade);
            e.Property(s => s.Balance).HasPrecision(18, 2);
            e.HasIndex(s => new { s.AssetId, s.Date }).IsUnique();
        });
    }
}
