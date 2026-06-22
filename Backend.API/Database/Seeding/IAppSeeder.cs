namespace Backend.Database.Seeding;

public interface IAppSeeder
{
    Task<SeedExecutionResult> SeedAsync(CancellationToken cancellationToken = default);
}
