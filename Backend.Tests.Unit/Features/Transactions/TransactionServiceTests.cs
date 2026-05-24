using Backend.Database;
using Backend.Features.Auth;
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

    private static UserWithVehiclesDto CreateUserDto(Guid userId, UserRole role = UserRole.Customer)
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
        var currentUser = Substitute.For<ICurrentUserContext>();
        var sut = new TransactionService(db, userService, currentUser);

        var actorUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        currentUser.GetRequiredUserId().Returns(actorUserId);
        currentUser.GetRequiredRole().Returns(UserRole.Customer);

        var dto = new CreateTransactionDto
        {
            UserId = targetUserId,
            Description = "Test",
            Amount = 10m
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.CreateTransactionAsync(dto));
        await userService.DidNotReceive().GetUserAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task CreateTransactionAsync_WhenAdminTargetsOtherUser_CreatesTransaction()
    {
        var db = CreateInMemoryDb();
        var userService = Substitute.For<IUserService>();
        var currentUser = Substitute.For<ICurrentUserContext>();
        var sut = new TransactionService(db, userService, currentUser);

        var actorUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        currentUser.GetRequiredUserId().Returns(actorUserId);
        currentUser.GetRequiredRole().Returns(UserRole.Admin);
        userService.GetUserAsync(targetUserId).Returns(CreateUserDto(targetUserId, UserRole.Customer));

        var dto = new CreateTransactionDto
        {
            UserId = targetUserId,
            Description = "Deposit",
            Amount = 30m
        };

        var result = await sut.CreateTransactionAsync(dto);

        Assert.Equal(targetUserId, result.UserId);
        Assert.Equal(dto.Amount, result.Amount);
        Assert.Single(db.Transactions);
    }

    [Fact]
    public async Task CreateTransactionAsync_WhenCurrentUserIsMissing_ThrowsUnauthorizedAccessException()
    {
        var db = CreateInMemoryDb();
        var userService = Substitute.For<IUserService>();
        var currentUser = Substitute.For<ICurrentUserContext>();
        var sut = new TransactionService(db, userService, currentUser);

        currentUser.GetRequiredUserId().Throws(new UnauthorizedAccessException());

        var dto = new CreateTransactionDto
        {
            Description = "Deposit",
            Amount = 15m
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.CreateTransactionAsync(dto));
    }

    [Fact]
    public async Task CreateTransactionAsync_WhenTargetUserNotFound_ThrowsKeyNotFoundException()
    {
        var db = CreateInMemoryDb();
        var userService = Substitute.For<IUserService>();
        var currentUser = Substitute.For<ICurrentUserContext>();
        var sut = new TransactionService(db, userService, currentUser);

        var actorUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        currentUser.GetRequiredUserId().Returns(actorUserId);
        currentUser.GetRequiredRole().Returns(UserRole.Admin);
        userService.GetUserAsync(targetUserId).ThrowsAsync<KeyNotFoundException>();

        var dto = new CreateTransactionDto
        {
            UserId = targetUserId,
            Description = "Deposit",
            Amount = 15m
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.CreateTransactionAsync(dto));
    }

    [Fact]
    public async Task CreateTransactionAsync_WhenTargetUserOmitted_DefaultsToCurrentUserAndPersistsTransaction()
    {
        var db = CreateInMemoryDb();
        var userService = Substitute.For<IUserService>();
        var currentUser = Substitute.For<ICurrentUserContext>();
        var sut = new TransactionService(db, userService, currentUser);

        var actorUserId = Guid.NewGuid();

        currentUser.GetRequiredUserId().Returns(actorUserId);
        currentUser.GetRequiredRole().Returns(UserRole.Customer);
        userService.GetUserAsync(actorUserId).Returns(CreateUserDto(actorUserId, UserRole.Customer));

        var dto = new CreateTransactionDto
        {
            Description = "Deposit",
            Amount = 99m
        };

        await sut.CreateTransactionAsync(dto);

        var transaction = await db.Transactions.SingleAsync();
        Assert.Equal(actorUserId, transaction.UserId);
        Assert.Equal(dto.Description, transaction.Description);
        Assert.Equal(dto.Amount, transaction.Amount);
        Assert.Equal(TransactionType.DEPOSIT, transaction.TransactionType);
    }

    [Fact]
    public async Task GetMyTransactionAsync_WhenNoTransactions_ReturnsEmptyList()
    {
        var db = CreateInMemoryDb();
        var userService = Substitute.For<IUserService>();
        var sut = new TransactionService(db, userService);

        var result = await sut.GetMyTransactionAsync(Guid.NewGuid());

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMyTransactionAsync_ReturnsOnlyUserTransactions()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        db.Transactions.Add(new Transaction { UserId = userId, Amount = 10, Description = "T1", TransactionType = TransactionType.DEPOSIT });
        db.Transactions.Add(new Transaction { UserId = userId, Amount = 20, Description = "T2", TransactionType = TransactionType.DEPOSIT });
        db.Transactions.Add(new Transaction { UserId = otherUserId, Amount = 5, Description = "T3", TransactionType = TransactionType.DEPOSIT });
        await db.SaveChangesAsync();

        var userService = Substitute.For<IUserService>();
        var sut = new TransactionService(db, userService);

        var result = await sut.GetMyTransactionAsync(userId);

        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.Equal(userId, t.UserId));
    }

    [Fact]
    public async Task GetMyTransactionAsync_ReturnsOrderedByCreatedAtDescending()
    {
        var db = CreateInMemoryDb();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        db.Transactions.Add(new Transaction { UserId = userId, Amount = 10, Description = "Old", TransactionType = TransactionType.DEPOSIT, CreatedAt = now.AddHours(-2) });
        db.Transactions.Add(new Transaction { UserId = userId, Amount = 20, Description = "New", TransactionType = TransactionType.DEPOSIT, CreatedAt = now });
        await db.SaveChangesAsync();

        var userService = Substitute.For<IUserService>();
        var sut = new TransactionService(db, userService);

        var result = await sut.GetMyTransactionAsync(userId);

        Assert.Equal("New", result[0].Description);
        Assert.Equal("Old", result[1].Description);
    }
}
