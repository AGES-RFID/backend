using System.Reflection;
using Backend.Features.Users;
<<<<<<< HEAD
using Backend.Features.Vehicles;
=======
using Backend.Features.Tags;
>>>>>>> b691c44 (feat: all tag routes implemented)
using Microsoft.EntityFrameworkCore;

namespace Backend.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
<<<<<<< HEAD
    public DbSet<Vehicle> Vehicles { get; set; }
=======
    public DbSet<Tag> Tags { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }

>>>>>>> b691c44 (feat: all tag routes implemented)
    public override int SaveChanges()
    {
        ApplyTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    // Method for auto populating the created_at and updated_at fields
    private void ApplyTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
                continue;

            var createdAtProp = entry.Metadata.FindProperty("CreatedAt");
            var updatedAtProp = entry.Metadata.FindProperty("UpdatedAt");

            if (createdAtProp is null || updatedAtProp is null)
                continue;

            if (createdAtProp.ClrType != typeof(DateTime) || updatedAtProp.ClrType != typeof(DateTime))
                continue;

            if (entry.State == EntityState.Added)
            {
                var createdAtValue = (DateTime)entry.Property("CreatedAt").CurrentValue!;
                if (createdAtValue == default)
                    entry.Property("CreatedAt").CurrentValue = now;

                entry.Property("UpdatedAt").CurrentValue = now;
            }
            else
            {
                entry.Property("CreatedAt").IsModified = false;
                entry.Property("UpdatedAt").CurrentValue = now;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresEnum<UserRole>("public", "user_role");
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
