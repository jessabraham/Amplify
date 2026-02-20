using Amplify.Domain.Entities.Trading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amplify.Infrastructure.Persistence.Configurations.Trading;

public class BacktestResultConfiguration : IEntityTypeConfiguration<BacktestResult>
{
    public void Configure(EntityTypeBuilder<BacktestResult> builder)
    {
        builder.ToTable("BacktestResults");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Asset).IsRequired().HasMaxLength(20);
        builder.Property(x => x.InitialCapital).HasColumnType("decimal(18,4)");
        builder.Property(x => x.WinRate).HasColumnType("decimal(5,2)");
        builder.Property(x => x.ProfitFactor).HasColumnType("decimal(8,4)");
        builder.Property(x => x.MaxDrawdown).HasColumnType("decimal(18,4)");
        builder.Property(x => x.NetPnL).HasColumnType("decimal(18,4)");
        builder.Property(x => x.SharpeRatio).HasColumnType("decimal(8,4)");
        builder.Property(x => x.ResultsJson).HasColumnType("nvarchar(max)");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}