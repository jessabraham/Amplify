using Amplify.Domain.Entities.Trading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amplify.Infrastructure.Persistence.Configurations.Trading;

public class TradeSignalConfiguration : IEntityTypeConfiguration<TradeSignal>
{
    public void Configure(EntityTypeBuilder<TradeSignal> builder)
    {
        builder.ToTable("TradeSignals");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Asset)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.SetupScore)
            .HasColumnType("decimal(5,2)");

        builder.Property(x => x.EntryPrice)
            .HasColumnType("decimal(18,4)");

        builder.Property(x => x.StopLoss)
            .HasColumnType("decimal(18,4)");

        builder.Property(x => x.Target1)
            .HasColumnType("decimal(18,4)");

        builder.Property(x => x.Target2)
            .HasColumnType("decimal(18,4)");

        builder.Property(x => x.RiskPercent)
            .HasColumnType("decimal(5,2)");

        builder.Property(x => x.AIAdvisoryJson)
            .HasColumnType("nvarchar(max)");

        // AI fields
        builder.Property(x => x.AIConfidence).HasPrecision(5, 2);
        builder.Property(x => x.AISummary).HasMaxLength(2000);
        builder.Property(x => x.AIBias).HasMaxLength(50);
        builder.Property(x => x.AIRecommendedAction).HasMaxLength(50);

        // Risk fields
        builder.Property(x => x.RiskPositionValue).HasPrecision(18, 2);
        builder.Property(x => x.RiskMaxLoss).HasPrecision(18, 2);
        builder.Property(x => x.RiskRewardRatio).HasPrecision(10, 4);
        builder.Property(x => x.RiskKellyPercent).HasPrecision(10, 4);
        builder.Property(x => x.RiskPortfolioSize).HasPrecision(18, 2);
        builder.Property(x => x.RiskWarnings).HasMaxLength(2000);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}