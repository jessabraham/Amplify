using Amplify.Domain.Entities.Trading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amplify.Infrastructure.Persistence.Configurations.Trading;

public class UserOverrideConfiguration : IEntityTypeConfiguration<UserOverride>
{
    public void Configure(EntityTypeBuilder<UserOverride> builder)
    {
        builder.ToTable("UserOverrides");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.ModifiedEntryPrice).HasColumnType("decimal(18,4)");
        builder.Property(x => x.ModifiedStopLoss).HasColumnType("decimal(18,4)");
        builder.Property(x => x.ModifiedTarget1).HasColumnType("decimal(18,4)");
        builder.Property(x => x.ModifiedTarget2).HasColumnType("decimal(18,4)");
        builder.Property(x => x.ActualPnL).HasColumnType("decimal(18,4)");

        builder.HasOne(x => x.TradeSignal)
            .WithMany()
            .HasForeignKey(x => x.TradeSignalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}