using Backend.Features.Dashboard;
using Backend.Features.Settings;
using Microsoft.Extensions.Configuration;
using Backend.Features.SystemConfig.Models;
using System.Linq;
using System.Collections.Generic;

namespace Backend.Features.SystemConfig;

public class SystemService(IDashboardService dashboardService, ISettingsService settingsService, IConfiguration configuration) : ISystemService
{
    private readonly IDashboardService _dashboardService = dashboardService;
    private readonly ISettingsService _settingsService = settingsService;
    private readonly IConfiguration _configuration = configuration;

    public async Task<SystemDto> GetSystemAsync()
    {
        var occupancy = await _dashboardService.GetOccupancyAsync();
        var maxOccupancy = await _settingsService.GetAsync("max_occupancy", 100);

        var antennas = new List<AntennaDto>();

        try
        {
            var cfg = _configuration.GetSection("Antennas").Get<List<AntennaConfig>>();
            if (cfg != null)
            {
                antennas = cfg.Select(a => new AntennaDto
                {
                    Id = a.Id,
                    Number = a.Number,
                    Status = a.Status ?? string.Empty,
                    Sensibility = a.Sensibility,
                    Power = a.Power
                }).ToList();
            }
        }
        catch
        {
            // ignore config parse errors and return empty antennas list
        }

        var system = new SystemDto
        {
            OccupancyLimit = maxOccupancy,
            CurrentOccupancy = occupancy.CurrentOccupancy,
            Antennas = antennas
        };

        return system;
    }
}
