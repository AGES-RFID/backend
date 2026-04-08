using Backend.Features.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Features.Users.Models;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.UserId);

        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(u => u.Email)
        .IsRequired()
        .HasMaxLength(255)
        .HasColumnType("varchar");

        builder.HasIndex(u => u.Email)
            .IsUnique();


        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.Cpf)
            .IsRequired()
            .HasMaxLength(11)
            .HasColumnType("varchar");

        builder.HasIndex(u => u.Cpf)
            .IsUnique();


        builder.Property(u => u.PhoneNumber)
            .IsRequired()
            .HasMaxLength(16)
            .HasColumnType("varchar");

        builder.HasIndex(u => u.PhoneNumber)
            .IsUnique();

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();


        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        builder.Property(u => u.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();
    }
}
