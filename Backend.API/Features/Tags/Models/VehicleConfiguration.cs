using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Features.Tags;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.HasKey(v => v.VehicleId);

        builder.Property(v => v.VehicleId)
            .IsRequired();

        builder.Property(v => v.LicensePlate)
            .IsRequired()
            .HasMaxLength(10);

        builder.HasIndex(v => v.LicensePlate)
            .IsUnique();

        builder.Property(v => v.Model)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.Color)
            .IsRequired()
            .HasMaxLength(50);

        builder.ToTable("Vehicles");
    }
}
