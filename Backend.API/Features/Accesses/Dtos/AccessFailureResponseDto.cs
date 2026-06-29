namespace Backend.Features.Accesses;

public sealed class AccessFailureResponseDto
{
    public bool Success { get; init; }
    public required string Reason { get; init; }
    public required string Message { get; init; }
    public string? Warning { get; init; }
}
