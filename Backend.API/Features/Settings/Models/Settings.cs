namespace Backend.Features.Settings;

public class Settings
{
    public Guid SettingsId { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Value { get; set; }
}