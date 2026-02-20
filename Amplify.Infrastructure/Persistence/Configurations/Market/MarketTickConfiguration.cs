using Amplify.Domain.Entities.Market;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amplify.Infrastructure.Persistence.Configurations.Market;

public class MarketTickConfiguration : IEntityTypeConfiguration<MarketTick>
{
    public void Configure(EntityTypeBuilder<MarketTick> builder)
    {
        builder.ToTable("MarketTicks");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Symbol).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Open).HasColumnType("decimal(18,4)");
        builder.Property(x => x.High).HasColumnType("decimal(18,4)");
        builder.Property(x => x.Low).HasColumnType("decimal(18,4)");
        builder.Property(x => x.Close).HasColumnType("decimal(18,4)");
    }
}