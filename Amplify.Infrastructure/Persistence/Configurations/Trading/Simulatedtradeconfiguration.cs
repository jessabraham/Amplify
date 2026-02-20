using Amplify.Domain.Entities.Trading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amplify.Infrastructure.Persistence.Configurations.Trading;

public class SimulatedTradeConfiguration : IEntityTypeConfiguration<SimulatedTrade>
{
    public void Configure(EntityTypeBuilder<SimulatedTrade> builder)
    {
        builder.ToTable("SimulatedTrades");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Asset).IsRequired().HasMaxLength(20);
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);

        // Prices
        builder.Property(x => x.EntryPrice).HasPrecision(18, 4);
        builder.Property(x => x.StopLoss).HasPrecision(18, 4);
        builder.Property(x => x.Target1).HasPrecision(18, 4);
        builder.Property(x => x.Target2).HasPrecision(18, 4);
        builder.Property(x => x.ExitPrice).HasPrecision(18, 4);
        builder.Property(x => x.HighestPriceSeen).HasPrecision(18, 4);
        builder.Property(x => x.LowestPriceSeen).HasPrecision(18, 4);

        // P&L
        builder.Property(x => x.PnLDollars).HasPrecision(18, 2);
        builder.Property(x => x.PnLPercent).HasPrecision(10, 4);
        builder.Property(x => x.RMultiple).HasPrecision(10, 4);
        builder.Property(x => x.MaxDrawdownPercent).HasPrecision(10, 4);

        // Position sizing
        builder.Property(x => x.PositionValue).HasPrecision(18, 2);
        builder.Property(x => x.MaxRisk).HasPrecision(18, 2);

        // Confidence scores
        builder.Property(x => x.PatternConfidence).HasPrecision(5, 2);
        builder.Property(x => x.AIConfidence).HasPrecision(5, 2);
        builder.Property(x => x.RSIAtEntry).HasPrecision(5, 2);

        // Strings
        builder.Property(x => x.PatternTimeframe).HasMaxLength(10);
        builder.Property(x => x.TimeframeAlignment).HasMaxLength(50);
        builder.Property(x => x.RegimeAlignment).HasMaxLength(50);
        builder.Property(x => x.MAAlignment).HasMaxLength(50);
        builder.Property(x => x.VolumeProfile).HasMaxLength(50);
        builder.Property(x => x.AIRecommendedAction).HasMaxLength(50);

        // Relationships
        builder.HasOne(x => x.TradeSignal)
            .WithMany()
            .HasForeignKey(x => x.TradeSignalId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index for querying active trades
        builder.HasIndex(x => new { x.UserId, x.Status });
        builder.HasIndex(x => x.TradeSignalId);
    }
}