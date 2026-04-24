using Backend.Features.Accesses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Features.Accesses.Models;

public class AccessesConfiguration : IEntityTypeConfiguration<Accesses>
{
    public void Configure(EntityTypeBuilder<Accesses> builder)
    {
        builder.HasKey(a => a.AccessesId);

        builder.Property(a => a.Type)
        .IsRequired()
        .HasColumnType("Acess_Type");

        builder.Property(a => a.Timestamp)
        .IsRequired()
        .HasDefaultValueSql("now()")
        .ValueGeneratedOnAdd();

        builder.HasOne(a => a.Tag)
            .WithMany()
            .HasForeignKey(a => a.TagId)
            .IsRequired(true);


        builder.Property(a => a.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        builder.Property(a => a.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();
    }
}