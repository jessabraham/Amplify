using Amplify.Domain.Entities.Trading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amplify.Infrastructure.Persistence.Configurations.Trading;

public class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("Positions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Symbol).IsRequired().HasMaxLength(20);
        builder.Property(x => x.EntryPrice).HasColumnType("decimal(18,4)");
        builder.Property(x => x.Quantity).HasColumnType("decimal(18,6)");
        builder.Property(x => x.ExitPrice).HasColumnType("decimal(18,4)");
        builder.Property(x => x.StopLoss).HasColumnType("decimal(18,4)");
        builder.Property(x => x.Target1).HasColumnType("decimal(18,4)");
        builder.Property(x => x.Target2).HasColumnType("decimal(18,4)");
        builder.Property(x => x.CurrentPrice).HasColumnType("decimal(18,4)");
        builder.Property(x => x.UnrealizedPnL).HasColumnType("decimal(18,4)");
        builder.Property(x => x.RealizedPnL).HasColumnType("decimal(18,4)");
        builder.Property(x => x.ReturnPercent).HasColumnType("decimal(8,4)");
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TradeSignal)
            .WithMany()
            .HasForeignKey(x => x.TradeSignalId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.UserId, x.Status });
        builder.HasIndex(x => x.Symbol);
    }
}