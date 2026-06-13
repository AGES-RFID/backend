using System.Reflection;
using Backend.Features.Tags;
using Backend.Features.Transactions;
using Backend.Features.Users;
using Backend.Features.Vehicles;
using Backend.Features.Accesses;
using Backend.Features.ParkingPrices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Backend.Features.Settings;

namespace Backend.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    private const string CreatedAtField = "CreatedAt";
    private const string UpdatedAtField = "UpdatedAt";

    public DbSet<User> Users { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<Access> Accesses { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<ParkingPrice> ParkingPrices { get; set; }
    public DbSet<Settings> Settings { get; set; }

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
            if (!IsValidEntry(entry)) continue;

            var createdAtProp = entry.Metadata.FindProperty(CreatedAtField);
            var updatedAtProp = entry.Metadata.FindProperty(UpdatedAtField);

            if (createdAtProp is null || updatedAtProp is null) continue;
            if (createdAtProp.ClrType != typeof(DateTime) || updatedAtProp.ClrType != typeof(DateTime)) continue;

            ApplyTimestampsForEntry(entry, now);
        }
    }

    private static bool IsValidEntry(EntityEntry entry)
        => entry.State is (EntityState.Added or EntityState.Modified);

    private void ApplyTimestampsForEntry(EntityEntry entry, DateTime now)
    {
        if (entry.State == EntityState.Added)
        {
            var createdAtValue = (DateTime)entry.Property(CreatedAtField).CurrentValue!;
            if (createdAtValue == default)
                entry.Property(CreatedAtField).CurrentValue = now;

            entry.Property(UpdatedAtField).CurrentValue = now;
        }
        else
        {
            entry.Property(CreatedAtField).IsModified = false;
            entry.Property(UpdatedAtField).CurrentValue = now;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresEnum<UserRole>("public", "user_role");
        modelBuilder.HasPostgresEnum<TransactionType>("public", "transaction_type");
        modelBuilder.HasPostgresEnum<AccessType>("public", "access_type");
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
