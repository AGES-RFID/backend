using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Backend.Features.Tags;

namespace Backend.Features.Vehicles.Models;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.HasKey(v => v.VehicleId);

        builder.HasIndex(v => v.Plate).IsUnique();

         builder.HasIndex(v => v.TagId).IsUnique();

        builder.HasOne(v => v.User)
               .WithMany()
               .HasForeignKey(v => v.UserId)
               .OnDelete(DeleteBehavior.Cascade);

         builder.HasOne(v => v.Tag)
             .WithOne(t => t.Vehicle)
             .HasForeignKey<Vehicle>(v => v.TagId)
             .IsRequired(false);

         builder.Property(v => v.TagId)
             .IsRequired(false);

        builder.Property(v => v.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        builder.Property(v => v.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();
    }
}
