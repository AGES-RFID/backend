namespace Backend.Database.Seeding;

public sealed class SeedExecutionResult
{
    public bool Skipped { get; init; }
    public string Message { get; init; } = string.Empty;

    public int UsersSeeded { get; init; }
    public int VehiclesSeeded { get; init; }
    public int TagsSeeded { get; init; }
    public int ParkingPricesSeeded { get; init; }
    public int TransactionsSeeded { get; init; }
    public int AccessesSeeded { get; init; }

    public static SeedExecutionResult SkippedExecution(string message) => new()
    {
        Skipped = true,
        Message = message
    };
}
