using Backend.Features.AccountBalance.Dtos;
using Backend.Features.AccountBalance.Models;
using Backend.Features.AccountBalance.Enums;

namespace Backend.Features.Common.Mapping;

public static class AccountBalanceMapper
{
    public static AccountBalanceDto ToDto(this AccountBalance accountBalance)
    {
        return new AccountBalanceDto
        {
            CustomerId = accountBalance.CustomerId,
            Balance = accountBalance.Balance,
            CreatedAt = accountBalance.CreatedAt,
            UpdatedAt = accountBalance.UpdatedAt
        };
    }

    public static TransactionDto ToDto(this Transaction transaction)
    {
        return new TransactionDto
        {
            Id = transaction.TransactionId,
            CustomerId = transaction.CustomerId,
            Amount = transaction.Amount,
            Type = transaction.Type,
            CreatedAt = transaction.CreatedAt,
            Description = transaction.Description,
            VehicleId = transaction.VehicleId,
            VehiclePlate = transaction.VehiclePlate
        };
    }

    public static Transaction ToEntity(this DepositDto dto, int customerId)
    {
        return new Transaction
        {
            CustomerId = customerId,
            Amount = dto.Amount,
            Type = Enums.TransactionType.Deposit,
            Description = dto.Description
        };
    }

    public static Transaction ToEntity(this WithdrawDto dto, int customerId)
    {
        return new Transaction
        {
            CustomerId = customerId,
            Amount = dto.Amount,
            Type = Enums.TransactionType.Withdraw,
            Description = dto.Description,
            VehicleId = dto.VehicleId,
            VehiclePlate = dto.VehiclePlate
        };
    }

    public static IEnumerable<TransactionDto> ToDtos(this IEnumerable<Transaction> transactions)
    {
        return transactions.Select(transaction => transaction.ToDto());
    }
}
