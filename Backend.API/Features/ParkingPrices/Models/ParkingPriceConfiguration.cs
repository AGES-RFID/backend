using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Features.ParkingPrices;

public class ParkingPricesConfiguration : IEntityTypeConfiguration<ParkingPrice>
{
    public void Configure(EntityTypeBuilder<ParkingPrice> builder)
    {
        builder.HasKey(p => p.ParkingPriceId);

        builder.Property(p => p.ToleranceMinutes)
            .IsRequired()
            .HasDefaultValue(15);

        builder.Property(p => p.BasePrice)
            .IsRequired()
            .HasColumnType("decimal(14,2)")
            .HasDefaultValue(15.00m);

        builder.Property(p => p.ThresholdMinutes)
            .IsRequired()
            .HasDefaultValue(3 * 60);

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

        builder.ToTable("parking_prices");
    }
}
