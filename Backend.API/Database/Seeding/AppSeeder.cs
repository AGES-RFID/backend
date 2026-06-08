using Backend.Features.Accesses;
using Backend.Features.ParkingPrices;
using Backend.Features.Settings;
using Backend.Features.Tags;
using Backend.Features.Tags.Enums;
using Backend.Features.Transactions;
using Backend.Features.Users;
using Backend.Features.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace Backend.Database.Seeding;

public class AppSeeder(AppDbContext db, ILogger<AppSeeder> logger) : IAppSeeder
{
    private readonly AppDbContext _db = db;
    private readonly ILogger<AppSeeder> _logger = logger;

    public async Task<SeedExecutionResult> SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await DatabaseHasAnyDataAsync(cancellationToken))
        {
            const string message = "Seeding skipped: database already contains data.";
            _logger.LogInformation(message);
            return SeedExecutionResult.SkippedExecution(message);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var now = DateTime.UtcNow;

        var adminUser = new User
        {
            Name = "Admin",
            Email = "admin@email.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = UserRole.Admin,
            Cpf = "11111111111",
            Cellphone = "+5551999999999"
        };

        var customerUser = new User
        {
            Name = "Cliente",
            Email = "cliente@email.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = UserRole.Customer,
            Cpf = "22222222222",
            Cellphone = "+5551888888888"
        };

        _db.Users.AddRange(adminUser, customerUser);
        await _db.SaveChangesAsync(cancellationToken);

        var primaryTag = new Tag
        {
            Epc = "E2000017221101441890ABCD",
            Tid = "TID-0001",
            Status = TagStatus.IN_USE
        };

        var secondaryTag = new Tag
        {
            Epc = "E2000017221101441890DCBA",
            Tid = "TID-0002",
            Status = TagStatus.IN_USE
        };

        _db.Tags.AddRange(primaryTag, secondaryTag);
        await _db.SaveChangesAsync(cancellationToken);

        var adminVehicle = new Vehicle
        {
            UserId = adminUser.UserId,
            TagId = primaryTag.TagId,
            Plate = "AAA1A11",
            Brand = "Toyota",
            Model = "Corolla"
        };

        var customerVehicle = new Vehicle
        {
            UserId = customerUser.UserId,
            TagId = secondaryTag.TagId,
            Plate = "BBB2B22",
            Brand = "Honda",
            Model = "Civic"
        };

        _db.Vehicles.AddRange(adminVehicle, customerVehicle);
        await _db.SaveChangesAsync(cancellationToken);

        var defaultPricing = new ParkingPrice
        {
            ToleranceMinutes = 15,
            BasePrice = 15m,
            HourlyRate = 5m,
            ThresholdMinutes = 180
        };

        _db.ParkingPrices.Add(defaultPricing);
        await _db.SaveChangesAsync(cancellationToken);

        var accessEntry = new Access
        {
            TagId = primaryTag.TagId,
            Tag = primaryTag,
            Type = AccessType.Entry,
            Timestamp = now.AddMinutes(-30)
        };

        var accessExit = new Access
        {
            TagId = primaryTag.TagId,
            Tag = primaryTag,
            Type = AccessType.Exit,
            Timestamp = now
        };

        _db.Accesses.AddRange(accessEntry, accessExit);
        await _db.SaveChangesAsync(cancellationToken);

        var depositTransaction = new Transaction
        {
            TransactionId = Guid.NewGuid(),
            UserId = customerUser.UserId,
            AccessId = accessEntry.AccessId,
            Amount = 100m,
            Description = "Initial wallet deposit",
            TransactionType = TransactionType.DEPOSIT
        };

        var withdrawalTransaction = new Transaction
        {
            TransactionId = Guid.NewGuid(),
            UserId = customerUser.UserId,
            AccessId = accessExit.AccessId,
            Amount = 15m,
            Description = "Parking payment",
            TransactionType = TransactionType.WITHDRAWAL
        };

        _db.Transactions.AddRange(depositTransaction, withdrawalTransaction);
        await _db.SaveChangesAsync(cancellationToken);

        var maxOccupancySetting = new Settings
        {
            Name = "max_occupancy",
            Value = "100"
        };

        _db.Settings.Add(maxOccupancySetting);
        await _db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        const string successMessage = "Seeding completed successfully.";
        _logger.LogInformation(successMessage);

        return new SeedExecutionResult
        {
            Message = successMessage,
            UsersSeeded = 2,
            TagsSeeded = 2,
            VehiclesSeeded = 2,
            ParkingPricesSeeded = 1,
            AccessesSeeded = 2,
            TransactionsSeeded = 2,
            SettingsSeeded = 1,
        };
    }

    private async Task<bool> DatabaseHasAnyDataAsync(CancellationToken cancellationToken)
    {
        return await _db.Users.AnyAsync(cancellationToken)
            || await _db.Vehicles.AnyAsync(cancellationToken)
            || await _db.Tags.AnyAsync(cancellationToken)
            || await _db.ParkingPrices.AnyAsync(cancellationToken)
            || await _db.Transactions.AnyAsync(cancellationToken)
            || await _db.Accesses.AnyAsync(cancellationToken);
    }
}
