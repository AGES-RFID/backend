using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Features.Tags.Models;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.HasKey(t => t.TagId);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(t => t.VeichleId)
            .IsUnique()
            .HasFilter("status = 'InUse'");

        builder.HasOne(t => t.Veichle)
            .WithMany(v => v.Tags)
            .HasForeignKey(t => t.VeichleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
