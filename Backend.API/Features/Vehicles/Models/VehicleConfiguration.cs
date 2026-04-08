using Backend.Features.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Features.Vehicles.Models;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.HasKey(v => v.VehicleId);

        builder.Property(v => v.Plate)
            .IsRequired()
            .HasColumnType("varchar");

        builder.HasIndex(v => v.Plate)
            .IsUnique();

        builder.Property(v => v.Brand)
            .IsRequired()
            .HasColumnType("varchar");

        builder.Property(v => v.Model)
            .IsRequired()
            .HasColumnType("varchar");

        builder.Property(v => v.Color)
            .IsRequired()
            .HasColumnType("varchar");

        builder.HasOne(v => v.User)
            .WithMany(u => u.Vehicles)
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
