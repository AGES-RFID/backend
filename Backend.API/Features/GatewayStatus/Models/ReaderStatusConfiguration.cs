using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Features.GatewayStatus;

public class ReaderStatusConfiguration : IEntityTypeConfiguration<ReaderStatus>
{
    public void Configure(EntityTypeBuilder<ReaderStatus> builder)
    {
        builder.ToTable("reader_status");

        builder.HasKey(r => r.ReaderId);

        builder.Property(r => r.ReaderId)
            .HasColumnName("reader_id");

        builder.Property(r => r.ReaderStatusValue)
            .HasColumnName("reader_status")
            .IsRequired();

        builder.Property(r => r.LastPing)
            .HasColumnName("last_ping")
            .IsRequired();

        builder.OwnsMany(r => r.AntennaList, antenna =>
        {
            antenna.ToJson("antenna_list");

            antenna.Property(a => a.Port)
                .HasJsonPropertyName("port");

            antenna.Property(a => a.Power)
                .HasJsonPropertyName("power");

            antenna.Property(a => a.Sensitivity)
                .HasJsonPropertyName("sensitivity");

            antenna.Property(a => a.AntennaStatus)
                .HasJsonPropertyName("antenna_status");
        });
    }
}
