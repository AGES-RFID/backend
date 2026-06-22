namespace Backend.Features.Accesses;

public sealed class AccessRegistrationConflictException(
    string reason,
    string message,
    string? warning = null) : InvalidOperationException(message)
{
    public string Reason { get; } = reason;
    public string? Warning { get; } = warning;
}
