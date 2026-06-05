using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Features.Settings;

public class SettingsConfiguration : IEntityTypeConfiguration<Settings>
{
    public void Configure(EntityTypeBuilder<Settings> builder)
    {
        builder.HasKey(s => s.SettingsId);

        builder.Property(s => s.MaxOccupancy)
            .IsRequired()
            .HasDefaultValue(100);

        builder.ToTable("settings");
    }
}