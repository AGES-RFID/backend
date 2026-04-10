using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Features.Accesses.Models;

public class AccessConfiguration : IEntityTypeConfiguration<Access>
{
    public void Configure(EntityTypeBuilder<Access> builder)
    {
        builder.HasKey(a => a.AccessId);

        builder.Property(a => a.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnType("access_type");

        builder.Property(a => a.Timestamp)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.HasOne(a => a.Tag)
            .WithMany(t => t.Accesses)
            .HasForeignKey(a => a.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
