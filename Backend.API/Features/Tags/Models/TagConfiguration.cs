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
            .HasColumnType("tag_status");

        builder.HasIndex(t => t.VehicleId)
            .IsUnique()
            .HasFilter("status = 'InUse'");

        builder.HasOne(t => t.Vehicle)
            .WithMany(v => v.Tags)
            .HasForeignKey(t => t.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
