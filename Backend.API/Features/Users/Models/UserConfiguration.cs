using Backend.Features.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Features.Users.Models;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.UserId);

        builder.HasMany(u => u.Vehicles).WithOne(v => v.User).HasForeignKey(v => v.UserId);

        builder.Property(u => u.Email)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.Name)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.Role)
            .IsRequired()
            .HasColumnType("user_role");

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
