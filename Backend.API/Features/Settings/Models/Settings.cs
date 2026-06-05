namespace Backend.Features.Settings;

public class Settings
{
    public Guid SettingsId { get; set; } = Guid.NewGuid();
    public int MaxOccupancy { get; set; } = 100;
}