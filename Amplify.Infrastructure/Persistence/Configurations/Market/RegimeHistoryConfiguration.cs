using Amplify.Domain.Entities.Market;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amplify.Infrastructure.Persistence.Configurations.Market;

public class RegimeHistoryConfiguration : IEntityTypeConfiguration<RegimeHistory>
{
    public void Configure(EntityTypeBuilder<RegimeHistory> builder)
    {
        builder.ToTable("RegimeHistory");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Symbol).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Confidence).HasColumnType("decimal(5,2)");
        builder.Property(x => x.FeatureVectorJson).HasColumnType("nvarchar(max)");
    }
}