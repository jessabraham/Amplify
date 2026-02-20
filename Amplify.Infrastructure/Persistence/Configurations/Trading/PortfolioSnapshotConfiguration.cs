using Amplify.Domain.Entities.Trading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amplify.Infrastructure.Persistence.Configurations.Trading;

public class PortfolioSnapshotConfiguration : IEntityTypeConfiguration<PortfolioSnapshot>
{
    public void Configure(EntityTypeBuilder<PortfolioSnapshot> builder)
    {
        builder.ToTable("PortfolioSnapshots");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TotalValue).HasColumnType("decimal(18,4)");
        builder.Property(x => x.CashBalance).HasColumnType("decimal(18,4)");
        builder.Property(x => x.InvestedAmount).HasColumnType("decimal(18,4)");
        builder.Property(x => x.DailyPnL).HasColumnType("decimal(18,4)");
        builder.Property(x => x.UnrealizedPnL).HasColumnType("decimal(18,4)");
        builder.Property(x => x.RealizedPnL).HasColumnType("decimal(18,4)");
        builder.Property(x => x.RiskExposurePercent).HasColumnType("decimal(5,2)");
        builder.Property(x => x.PositionsJson).HasColumnType("nvarchar(max)");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}