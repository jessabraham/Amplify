using Amplify.Domain.Entities.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Amplify.Infrastructure.Persistence.Configurations.AI;

public class AIAnalyticConfiguration : IEntityTypeConfiguration<AIAnalytic>
{
    public void Configure(EntityTypeBuilder<AIAnalytic> builder)
    {
        builder.ToTable("AIAnalytics");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ModelName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.PromptSent).HasColumnType("nvarchar(max)");
        builder.Property(x => x.ResponseReceived).HasColumnType("nvarchar(max)");

        builder.HasOne(x => x.TradeSignal)
            .WithMany()
            .HasForeignKey(x => x.TradeSignalId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
