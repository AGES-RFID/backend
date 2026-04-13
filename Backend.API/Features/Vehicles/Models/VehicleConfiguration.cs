using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Features.Vehicles.Models;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.HasKey(v => v.VehicleId);

        builder.HasIndex(v => v.Plate).IsUnique();

        builder.HasOne(v => v.User)
               .WithMany()
               .HasForeignKey(v => v.UserId)
               .OnDelete(DeleteBehavior.Cascade);

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
