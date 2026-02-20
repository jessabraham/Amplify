using Amplify.Domain.Entities.Trading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amplify.Infrastructure.Persistence.Configurations.Trading;

public class WatchlistItemConfiguration : IEntityTypeConfiguration<WatchlistItem>
{
    public void Configure(EntityTypeBuilder<WatchlistItem> builder)
    {
        builder.ToTable("WatchlistItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Symbol).IsRequired().HasMaxLength(20);
        builder.Property(x => x.MinConfidence).HasColumnType("decimal(5,2)");
        builder.Property(x => x.LastBias).HasMaxLength(50);

        builder.HasIndex(x => new { x.UserId, x.Symbol }).IsUnique();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}