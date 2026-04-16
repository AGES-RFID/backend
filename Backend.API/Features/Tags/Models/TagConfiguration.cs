using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Backend.Features.Tags.Enums;
using Backend.Features.Vehicles;

namespace Backend.Features.Tags;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.HasKey(t => t.TagId);

        builder.Property(t => t.TagId)
            .IsRequired();

        builder.Property(t => t.Status)
            .IsRequired()
            .HasDefaultValue(TagStatus.AVAILABLE)
            .HasConversion<int>();

        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("now()");

        builder.Property(t => t.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("now()");

        builder.ToTable("Tags");
    }
}
