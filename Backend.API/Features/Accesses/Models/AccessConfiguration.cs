using Backend.Features.Accesses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Features.Accesses.Models;

public class AccessesConfiguration : IEntityTypeConfiguration<Access>
{
    public void Configure(EntityTypeBuilder<Access> builder)
    {
        builder.HasKey(a => a.AccessId);

        builder.Property(a => a.Type)
        .IsRequired()
        .HasColumnType("access_type");

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