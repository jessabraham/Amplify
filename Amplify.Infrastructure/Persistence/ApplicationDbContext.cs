using Amplify.Domain.Entities.AI;
using Amplify.Domain.Entities.Identity;
using Amplify.Domain.Entities.Market;
using Amplify.Domain.Entities.Trading;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;



namespace Amplify.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // Trading
    public DbSet<TradeSignal> TradeSignals => Set<TradeSignal>();
    public DbSet<UserOverride> UserOverrides => Set<UserOverride>();
    public DbSet<BacktestResult> BacktestResults => Set<BacktestResult>();
    public DbSet<PortfolioSnapshot> PortfolioSnapshots => Set<PortfolioSnapshot>();

    // Market
    public DbSet<MarketTick> MarketTicks => Set<MarketTick>();
    public DbSet<RegimeHistory> RegimeHistory => Set<RegimeHistory>();
    public DbSet<FeatureVector> FeatureVectors => Set<FeatureVector>();

    // AI
    public DbSet<AIAnalytic> AIAnalytics => Set<AIAnalytic>();
    public DbSet<DetectedPattern> DetectedPatterns => Set<DetectedPattern>();
    public DbSet<WatchlistItem> WatchlistItems => Set<WatchlistItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}