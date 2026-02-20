using Amplify.Domain.Entities.Trading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amplify.Infrastructure.Persistence.Configurations.Trading;

public class PatternPerformanceConfiguration : IEntityTypeConfiguration<PatternPerformance>
{
    public void Configure(EntityTypeBuilder<PatternPerformance> builder)
    {
        builder.ToTable("PatternPerformances");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
        builder.Property(x => x.Timeframe).IsRequired().HasMaxLength(10);

        // Rates and percentages
        builder.Property(x => x.WinRate).HasPrecision(5, 2);
        builder.Property(x => x.AvgWinPercent).HasPrecision(10, 4);
        builder.Property(x => x.AvgLossPercent).HasPrecision(10, 4);
        builder.Property(x => x.AvgRMultiple).HasPrecision(10, 4);
        builder.Property(x => x.BestTradePercent).HasPrecision(10, 4);
        builder.Property(x => x.WorstTradePercent).HasPrecision(10, 4);
        builder.Property(x => x.TotalPnLPercent).HasPrecision(12, 4);
        builder.Property(x => x.ProfitFactor).HasPrecision(10, 4);
        builder.Property(x => x.AvgDaysHeld).HasPrecision(8, 2);

        // Context-specific rates
        builder.Property(x => x.WinRateWhenAligned).HasPrecision(5, 2);
        builder.Property(x => x.WinRateWhenConflicting).HasPrecision(5, 2);
        builder.Property(x => x.WinRateWithBreakoutVol).HasPrecision(5, 2);

        // Unique combo per user
        builder.HasIndex(x => new { x.UserId, x.PatternType, x.Direction, x.Timeframe, x.Regime })
            .IsUnique();
    }
}