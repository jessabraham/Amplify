using Amplify.Domain.Entities.Market;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Amplify.Infrastructure.Persistence.Configurations.Market;

public class FeatureVectorConfiguration : IEntityTypeConfiguration<FeatureVector>
{
    public void Configure(EntityTypeBuilder<FeatureVector> builder)
    {
        builder.ToTable("FeatureVectors");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Symbol).IsRequired().HasMaxLength(20);
        builder.Property(x => x.RSI).HasColumnType("decimal(8,4)");
        builder.Property(x => x.MACD).HasColumnType("decimal(18,4)");
        builder.Property(x => x.MACDSignal).HasColumnType("decimal(18,4)");
        builder.Property(x => x.BollingerUpper).HasColumnType("decimal(18,4)");
        builder.Property(x => x.BollingerLower).HasColumnType("decimal(18,4)");
        builder.Property(x => x.ATR).HasColumnType("decimal(18,4)");
        builder.Property(x => x.SMA20).HasColumnType("decimal(18,4)");
        builder.Property(x => x.SMA50).HasColumnType("decimal(18,4)");
        builder.Property(x => x.EMA12).HasColumnType("decimal(18,4)");
        builder.Property(x => x.EMA26).HasColumnType("decimal(18,4)");
        builder.Property(x => x.VWAP).HasColumnType("decimal(18,4)");
    }
}