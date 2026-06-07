using Backend.Database;
using Backend.Features.Settings;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Unit.Features.SettingsTests;

public class SettingsServiceTests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static Settings CreateSetting(string name, string value)
    {
        return new Settings
        {
            Name = name,
            Value = value
        };
    }

    // GetAsync
    [Fact]
    public async Task GetAsync_WhenSettingExists_ReturnsValue()
    {
        var db = CreateInMemoryDb();
        var setting = CreateSetting("max_occupancy", "100");
        db.Settings.Add(setting);
        await db.SaveChangesAsync();

        var service = new SettingsService(db);
        var result = await service.GetAsync<string>(setting.Name);

        Assert.IsType<string>(result);
        Assert.Equal(setting.Value, result);
    }

    [Fact]
    public async Task GetAsync_WhenSettingExists_ParsesAndReturnsValue()
    {
        var db = CreateInMemoryDb();
        var setting = CreateSetting("max_occupancy", "100");
        db.Settings.Add(setting);
        await db.SaveChangesAsync();

        var service = new SettingsService(db);
        var result = await service.GetAsync<int>(setting.Name);

        Assert.IsType<int>(result);
        Assert.Equal(100, result);
    }

    [Fact]
    public async Task GetAsync_WhenSettingExists_ButParsingFails_ThrowsFormatException()
    {
        var db = CreateInMemoryDb();
        var setting = CreateSetting("max_occupancy", "not_a_number");
        db.Settings.Add(setting);
        await db.SaveChangesAsync();

        var service = new SettingsService(db);
        await Assert.ThrowsAsync<FormatException>(() => service.GetAsync<int>(setting.Name));
    }

    [Fact]
    public async Task GetAsync_WhenSettingNotFound_ThrowsKeyNotFoundException()
    {
        var db = CreateInMemoryDb();
        var service = new SettingsService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetAsync<string>("nonexistent"));
    }

    [Fact]
    public async Task GetAsync_WithDefault_WhenSettingExists_ReturnsValue()
    {
        var db = CreateInMemoryDb();
        var setting = CreateSetting("theme", "dark");
        db.Settings.Add(setting);
        await db.SaveChangesAsync();

        var service = new SettingsService(db);
        var result = await service.GetAsync("theme", "light");

        Assert.Equal("dark", result);
    }

    [Fact]
    public async Task GetAsync_WithDefault_WhenSettingNotFound_ReturnsDefault()
    {
        var db = CreateInMemoryDb();
        var service = new SettingsService(db);

        var result = await service.GetAsync("max_occupancy", "50");

        Assert.Equal("50", result);
    }

    [Fact]
    public async Task GetAsync_WithNonStringDefault_WhenSettingNotFound_ReturnsDefault()
    {
        var db = CreateInMemoryDb();
        var service = new SettingsService(db);

        var result = await service.GetAsync("max_occupancy", 50);

        Assert.Equal(50, result);
    }

    // SetAsync — Create
    [Fact]
    public async Task SetAsync_WhenNewSetting_CreatesAndReturnsValue()
    {
        var db = CreateInMemoryDb();
        var service = new SettingsService(db);

        var result = await service.SetAsync("max_occupancy", "200");

        Assert.Equal("200", result);

        var saved = await db.Settings.FirstOrDefaultAsync(s => s.Name == "max_occupancy");
        Assert.NotNull(saved);
        Assert.Equal("200", saved.Value);
    }

    // SetAsync — Update
    [Fact]
    public async Task SetAsync_WhenExistingSetting_UpdatesAndReturnsValue()
    {
        var db = CreateInMemoryDb();
        db.Settings.Add(CreateSetting("max_occupancy", "100"));
        await db.SaveChangesAsync();

        var service = new SettingsService(db);
        var result = await service.SetAsync("max_occupancy", "300");

        Assert.Equal("300", result);

        var saved = await db.Settings.FirstAsync(s => s.Name == "max_occupancy");
        Assert.Equal("300", saved.Value);
    }

    [Fact]
    public async Task SetAsync_WithMultipleSettings_DoesNotAffectOthers()
    {
        var db = CreateInMemoryDb();
        db.Settings.Add(CreateSetting("theme", "light"));
        db.Settings.Add(CreateSetting("language", "en"));
        await db.SaveChangesAsync();

        var service = new SettingsService(db);
        await service.SetAsync("language", "pt-BR");

        var theme = await db.Settings.FirstAsync(s => s.Name == "theme");
        Assert.Equal("light", theme.Value);

        var language = await db.Settings.FirstAsync(s => s.Name == "language");
        Assert.Equal("pt-BR", language.Value);
    }
}
