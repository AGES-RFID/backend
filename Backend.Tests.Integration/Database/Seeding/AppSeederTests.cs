using Backend.Database;
using Backend.Database.Seeding;
using Backend.Features.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using tests.Setup;

namespace Backend.Tests.Integration.Database.Seeding;

public class AppSeederTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory = factory;
    private readonly IServiceScopeFactory _scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SeedAsync_WhenDatabaseIsEmpty_ShouldSeedAllTables()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<IAppSeeder>();

        var result = await seeder.SeedAsync();

        Assert.False(result.Skipped);
        Assert.True(await db.Users.AnyAsync());
        Assert.True(await db.Vehicles.AnyAsync());
        Assert.True(await db.Tags.AnyAsync());
        Assert.True(await db.ParkingPrices.AnyAsync());
        Assert.True(await db.Transactions.AnyAsync());
        Assert.True(await db.Accesses.AnyAsync());
    }

    [Fact]
    public async Task SeedAsync_WhenAnyTableHasData_ShouldSkipEntireSeeding()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<IAppSeeder>();

        db.Users.Add(new User
        {
            Name = "PreExisting",
            Email = "preexisting@backend.local",
            PasswordHash = "hash",
            Role = UserRole.Admin
        });
        await db.SaveChangesAsync();

        var result = await seeder.SeedAsync();

        Assert.True(result.Skipped);
        Assert.Equal(1, await db.Users.CountAsync());
        Assert.Equal(0, await db.Vehicles.CountAsync());
        Assert.Equal(0, await db.Tags.CountAsync());
        Assert.Equal(0, await db.ParkingPrices.CountAsync());
        Assert.Equal(0, await db.Transactions.CountAsync());
        Assert.Equal(0, await db.Accesses.CountAsync());
    }
}
