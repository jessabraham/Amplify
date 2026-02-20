using Amplify.Domain.Entities.Trading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amplify.Infrastructure.Persistence.Configurations.Trading;

public class DetectedPatternConfiguration : IEntityTypeConfiguration<DetectedPattern>
{
    public void Configure(EntityTypeBuilder<DetectedPattern> builder)
    {
        builder.ToTable("DetectedPatterns");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Asset).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.AIAnalysis).HasColumnType("nvarchar(max)");
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);

        // Decimal precision
        builder.Property(x => x.Confidence).HasColumnType("decimal(5,1)");
        builder.Property(x => x.HistoricalWinRate).HasColumnType("decimal(5,1)");
        builder.Property(x => x.DetectedAtPrice).HasColumnType("decimal(18,2)");
        builder.Property(x => x.SuggestedEntry).HasColumnType("decimal(18,2)");
        builder.Property(x => x.SuggestedStop).HasColumnType("decimal(18,2)");
        builder.Property(x => x.SuggestedTarget).HasColumnType("decimal(18,2)");
        builder.Property(x => x.AIConfidence).HasColumnType("decimal(5,1)");
        builder.Property(x => x.ActualPnLPercent).HasColumnType("decimal(8,2)");

        // Enum storage as string
        builder.Property(x => x.PatternType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Direction).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Timeframe).HasConversion<string>().HasMaxLength(20);
    }
}