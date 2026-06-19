using Backend.Features.Dashboard;
using Microsoft.Extensions.Configuration;
using Backend.Features.SystemConfig.Models;
using System.Linq;
using System.Collections.Generic;

namespace Backend.Features.SystemConfig;

public class SystemService(IDashboardService dashboardService, IConfiguration configuration) : ISystemService
{
    private readonly IDashboardService _dashboardService = dashboardService;
    private readonly IConfiguration _configuration = configuration;

    public async Task<SystemDto> GetSystemAsync()
    {
        var occupancy = await _dashboardService.GetOccupancyAsync();
        var antennas = await GetAntennasAsync();

        var system = new SystemDto
        {
            OccupancyLimit = occupancy.MaxOccupancy,
            CurrentOccupancy = occupancy.CurrentOccupancy,
            Antennas = antennas
        };

        return system;
    }

    public Task<List<AntennaDto>> GetAntennasAsync()
    {
        var antennas = new List<AntennaDto>();

        try
        {
            var cfg = _configuration.GetSection("Antennas").Get<List<AntennaConfig>>();
            if (cfg != null)
            {
                antennas = cfg.Select(a => new AntennaDto
                {
                    Id = a.Id,
                    Name = $"Antena {a.Number}",
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

        return Task.FromResult(antennas);
    }
}
