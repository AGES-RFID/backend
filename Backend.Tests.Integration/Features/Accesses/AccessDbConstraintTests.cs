using Backend.Database;
using Backend.Features.Accesses;
using Backend.Features.Tags;
using Backend.Features.Users;
using Backend.Features.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using tests.Setup;

namespace tests.Features.Accesses;

public class AccessDbConstraintTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public AccessDbConstraintTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private AppDbContext CreateDbContext()
    {
        var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    private static async Task<Tag> CreateTagAsync(AppDbContext db, string suffix = "")
    {
        var user = new User
        {
            Name = "Test User",
            Email = $"user{suffix}@test.com",
            PasswordHash = "hash",
            Cpf = $"{suffix.PadLeft(11, '0')}",
            PhoneNumber = $"{suffix.PadLeft(16, '0')}",
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var vehicle = new Vehicle
        {
            UserId = user.UserId,
            Plate = $"ACT{suffix}",
            Brand = "Ford",
            Model = "Focus",
            Color = "Blue",
        };
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        var tag = new Tag { VeichleId = vehicle.VehicleId, Status = TagStatus.InUse };
        db.Tags.Add(tag);
        await db.SaveChangesAsync();

        return tag;
    }

    [Fact]
    public async Task CreateAccess_ForTag_ShouldPersist()
    {
        var db = CreateDbContext();
        var tag = await CreateTagAsync(db, "1");

        var access = new Access
        {
            TagId = tag.TagId,
            Type = AccessType.Entry,
            Timestamp = DateTime.UtcNow,
        };
        db.Accesses.Add(access);
        await db.SaveChangesAsync();

        var persisted = await db.Accesses.FindAsync(access.AccessId);
        Assert.NotNull(persisted);
        Assert.Equal(tag.TagId, persisted.TagId);
        Assert.Equal(AccessType.Entry, persisted.Type);
    }

    [Fact]
    public async Task CreateMultipleAccesses_ForSameTag_ShouldPersist()
    {
        var db = CreateDbContext();
        var tag = await CreateTagAsync(db, "2");

        db.Accesses.Add(new Access { TagId = tag.TagId, Type = AccessType.Entry, Timestamp = DateTime.UtcNow });
        db.Accesses.Add(new Access { TagId = tag.TagId, Type = AccessType.Exit, Timestamp = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var accesses = await db.Accesses.Where(a => a.TagId == tag.TagId).ToListAsync();
        Assert.Equal(2, accesses.Count);
    }

    [Fact]
    public async Task DeleteTag_ShouldCascadeDeleteAccesses()
    {
        var db = CreateDbContext();
        var tag = await CreateTagAsync(db, "3");

        db.Accesses.Add(new Access { TagId = tag.TagId, Type = AccessType.Entry, Timestamp = DateTime.UtcNow });
        db.Accesses.Add(new Access { TagId = tag.TagId, Type = AccessType.Exit, Timestamp = DateTime.UtcNow });
        await db.SaveChangesAsync();

        db.Tags.Remove(tag);
        await db.SaveChangesAsync();

        var remaining = await db.Accesses.Where(a => a.TagId == tag.TagId).ToListAsync();
        Assert.Empty(remaining);
    }
}
