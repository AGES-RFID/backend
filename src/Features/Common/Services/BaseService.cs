namespace Backend.Features.Common.Services;

public abstract class BaseService
{
    protected int GenerateId<T>(IList<T> collection)
    {
        return collection.Count + 1;
    }

    protected void ValidateNotNull<T>(T entity, string entityName, int id)
    {
        if (entity == null)
            throw new KeyNotFoundException($"{entityName} com ID {id} não encontrado");
    }

    protected void ValidateExists<T>(T entity, string entityName, string identifier)
    {
        if (entity != null)
            throw new InvalidOperationException($"{entityName} {identifier} já existe");
    }

    protected void ValidateActive(bool isActive, string entityName, int id)
    {
        if (!isActive)
            throw new InvalidOperationException($"{entityName} com ID {id} já está desativado");
    }

    protected void ValidateInactive(bool isActive, string entityName, int id)
    {
        if (isActive)
            throw new InvalidOperationException($"{entityName} com ID {id} já está ativo");
    }

    protected void ValidateSufficientFunds(decimal currentBalance, decimal amount)
    {
        if (currentBalance < amount)
            throw new InvalidOperationException("Fundos insuficientes");
    }

    protected void ValidateHasEntry(DateTime? lastEntry, string entityName, int id)
    {
        if (!lastEntry.HasValue)
            throw new InvalidOperationException($"{entityName} com ID {id} não possui registro de entrada");
    }

    protected DateTime UpdateTimestamp()
    {
        return DateTime.UtcNow;
    }

    protected TimeSpan? CalculateDuration(DateTime? entryTime, DateTime? exitTime)
    {
        if (entryTime.HasValue && !exitTime.HasValue)
        {
            return DateTime.UtcNow - entryTime.Value;
        }
        
        if (entryTime.HasValue && exitTime.HasValue)
        {
            return exitTime.Value - entryTime.Value;
        }

        return null;
    }
}
