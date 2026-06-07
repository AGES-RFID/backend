using Backend.Database;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Settings;

public interface ISettingsService
{
    Task<T> GetAsync<T>(string name, T? defaultValue = default) where T : IParsable<T>;
    Task<string> SetAsync(string name, string value);
}

public class SettingsService(AppDbContext db) : ISettingsService
{
    private readonly AppDbContext _db = db;

    public async Task<T> GetAsync<T>(string name, T? defaultValue = default)
    where T : IParsable<T>
    {
        var setting = await _db.Settings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Name == name);

        if (setting is null)
        {
            return defaultValue ?? throw new KeyNotFoundException($"Setting '{name}' not found and no default value provided.");
        }

        return T.Parse(setting.Value, null);
    }

    public async Task<string> SetAsync(string name, string value)
    {
        var existing = await _db.Settings
            .FirstOrDefaultAsync(s => s.Name == name);

        if (existing is not null)
        {
            existing.Value = value;
        }
        else
        {
            existing = new Settings
            {
                Name = name,
                Value = value
            };

            _db.Settings.Add(existing);
        }

        await _db.SaveChangesAsync();

        return existing.Value;
    }
}
