using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Features.ParkingSettings;

public class ParkingSettingsConfiguration : IEntityTypeConfiguration<ParkingSettings>
{
    public void Configure(EntityTypeBuilder<ParkingSettings> builder)
    {
        builder.HasKey(p => p.ParkingSettingsId);

        builder.Property(p => p.ToleranceMinutes)
            .IsRequired()
            .HasDefaultValue(15);

        builder.Property(p => p.BasePrice)
            .IsRequired()
            .HasColumnType("decimal(14,2)")
            .HasDefaultValue(15.00m);

        builder.Property(p => p.HourlyRate)
            .IsRequired()
            .HasColumnType("decimal(14,2)")
            .HasDefaultValue(5.00m);

        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        builder.Property(p => p.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        builder.ToTable("parking_settings");
    }
}