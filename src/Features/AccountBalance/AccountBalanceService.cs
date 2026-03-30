using Backend.Features.Common.Mapping;
using Backend.Features.AccountBalance.Enums;
using Backend.Features.Common.Services;

public interface IAccountBalanceService
{
    Task<AccountBalanceDto> GetBalanceAsync(int customerId);
    Task<IEnumerable<TransactionDto>> GetTransactionsAsync(int customerId);
    Task<TransactionDto> DepositAsync(int customerId, DepositDto dto);
    Task<TransactionDto> WithdrawAsync(int customerId, WithdrawDto dto);
}

public class AccountBalanceService : BaseService, IAccountBalanceService
{
    private readonly List<Backend.Features.AccountBalance.Models.AccountBalance> _accountBalances = new();
    private readonly List<Backend.Features.AccountBalance.Models.Transaction> _transactions = new();

    public async Task<AccountBalanceDto> GetBalanceAsync(int customerId)
    {
        var accountBalance = _accountBalances.FirstOrDefault(ab => ab.CustomerId == customerId);
        if (accountBalance == null)
        {
            accountBalance = new Backend.Features.AccountBalance.Models.AccountBalance
            {
                CustomerId = customerId,
                AccountBalanceId = _accountBalances.Count + 1
            };
            _accountBalances.Add(accountBalance);
        }

        return accountBalance.ToDto();
    }

    public async Task<IEnumerable<TransactionDto>> GetTransactionsAsync(int customerId)
    {
        var customerTransactions = _transactions.Where(t => t.CustomerId == customerId)
            .OrderByDescending(t => t.CreatedAt);

        return customerTransactions.ToDtos();
    }

    public async Task<TransactionDto> DepositAsync(int customerId, DepositDto dto)
    {
        var accountBalance = _accountBalances.FirstOrDefault(ab => ab.CustomerId == customerId);
        if (accountBalance == null)
        {
            accountBalance = new Backend.Features.AccountBalance.Models.AccountBalance
            {
                CustomerId = customerId,
                AccountBalanceId = GenerateId(_accountBalances)
            };
            _accountBalances.Add(accountBalance);
        }

        var transaction = dto.ToEntity(customerId);
        transaction.TransactionId = GenerateId(_transactions);

        _transactions.Add(transaction);

        accountBalance.Balance += dto.Amount;
        accountBalance.UpdatedAt = DateTime.UtcNow;

        return transaction.ToDto();
    }

    public async Task<TransactionDto> WithdrawAsync(int customerId, WithdrawDto dto)
    {
        var accountBalance = _accountBalances.FirstOrDefault(ab => ab.CustomerId == customerId);
        ValidateNotNull(accountBalance, "Saldo da conta", customerId);

        ValidateSufficientFunds(accountBalance.Balance, dto.Amount);

        var transaction = dto.ToEntity(customerId);
        transaction.TransactionId = GenerateId(_transactions);

        _transactions.Add(transaction);

        accountBalance.Balance -= dto.Amount;
        accountBalance.UpdatedAt = DateTime.UtcNow;

        return transaction.ToDto();
    }
}
