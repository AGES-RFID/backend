using Backend.Database;
using Backend.Features.Transactions;
using Backend.Features.Users;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Backend.Tests.Unit.Features.Transactions;

public class TransactionServiceTests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static UserDto CreateUserDto(Guid userId, UserRole role = UserRole.Customer)
        => new()
        {
            UserId = userId,
            Name = "Test",
            Email = "test@example.com",
            Role = role
        };

    [Fact]
    public async Task CreateTransactionAsync_WhenNonAdminTargetsOtherUser_ThrowsUnauthorized()
    {
        var db = CreateInMemoryDb();
        var userService = Substitute.For<IUserService>();
        var sut = new TransactionService(db, userService);

        var actorUserId = Guid.NewGuid();
        userService.GetUserAsync(actorUserId).Returns(CreateUserDto(actorUserId, UserRole.Customer));
        var targetUserId = Guid.NewGuid();

        var command = new CreateTransactionCommand
        {
            ActorUserId = actorUserId,
            TargetUserId = targetUserId,
            Description = "Test",
            Amount = 10m
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.CreateTransactionAsync(command));
        await userService.Received(1).GetUserAsync(actorUserId);
        await userService.DidNotReceive().GetUserAsync(targetUserId);
    }

    [Fact]
    public async Task CreateTransactionAsync_WhenAdminTargetsOtherUser_CreatesTransaction()
    {
        var db = CreateInMemoryDb();
        var userService = Substitute.For<IUserService>();
        var sut = new TransactionService(db, userService);

        var actorUserId = Guid.NewGuid();
        userService.GetUserAsync(actorUserId).Returns(CreateUserDto(actorUserId, UserRole.Admin));
        var targetUserId = Guid.NewGuid();
        userService.GetUserAsync(targetUserId).Returns(CreateUserDto(targetUserId, UserRole.Customer));

        var command = new CreateTransactionCommand
        {
            ActorUserId = actorUserId,
            TargetUserId = targetUserId,
            Description = "Deposit",
            Amount = 30m
        };

        var result = await sut.CreateTransactionAsync(command);

        Assert.Equal(targetUserId, result.UserId);
        Assert.Equal(command.Amount, result.Amount);
        Assert.Single(db.Transactions);
    }

    [Fact]
    public async Task CreateTransactionAsync_WhenActorNotFound_ThrowsInvalidOperationException()
    {
        var db = CreateInMemoryDb();
        var userService = Substitute.For<IUserService>();
        var sut = new TransactionService(db, userService);

        var actorUserId = Guid.NewGuid();
        userService.GetUserAsync(actorUserId).ThrowsAsync<KeyNotFoundException>();

        var command = new CreateTransactionCommand
        {
            ActorUserId = actorUserId,
            TargetUserId = Guid.NewGuid(),
            Description = "Deposit",
            Amount = 15m
        };

        await Assert.ThrowsAnyAsync<Exception>(() => sut.CreateTransactionAsync(command));
    }

    [Fact]
    public async Task CreateTransactionAsync_WhenValid_PersistsTransactionFields()
    {
        var db = CreateInMemoryDb();
        var userService = Substitute.For<IUserService>();
        var sut = new TransactionService(db, userService);

        var targetUserId = Guid.NewGuid();
        userService.GetUserAsync(targetUserId).Returns(CreateUserDto(targetUserId, UserRole.Customer));

        var command = new CreateTransactionCommand
        {
            ActorUserId = targetUserId,
            TargetUserId = targetUserId,
            Description = "Deposit",
            Amount = 99m
        };

        await sut.CreateTransactionAsync(command);

        var transaction = await db.Transactions.SingleAsync();
        Assert.Equal(targetUserId, transaction.UserId);
        Assert.Equal(command.Description, transaction.Description);
        Assert.Equal(command.Amount, transaction.Amount);
        Assert.Equal(TransactionType.DEPOSIT, transaction.TransactionType);
    }
}
