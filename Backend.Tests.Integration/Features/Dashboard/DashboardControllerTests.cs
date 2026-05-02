using System.Net;
using System.Net.Http.Json;
using Backend.Features.Dashboard;
using tests.Setup;

namespace tests.Features.Dashboard;

public class DashboardControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetMetrics_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/api/dashboard/metrics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMetrics_WhenNoAccesses_ShouldReturnZerosAndNullPeakTime()
    {
        var response = await _client.GetAsync("/api/dashboard/metrics");
        response.EnsureSuccessStatusCode();

        var metrics = await response.Content.ReadFromJsonAsync<DashboardMetricsDto>(CustomWebApplicationFactory.JsonOptions);

        Assert.NotNull(metrics);
        Assert.Equal(0, metrics.EntriesLastHour);
        Assert.Equal(0, metrics.ExitsLastHour);
        Assert.Null(metrics.PeakEntryTime);
    }

    [Fact]
    public async Task GetMetrics_ShouldReturnCorrectFields()
    {
        var response = await _client.GetAsync("/api/dashboard/metrics");
        response.EnsureSuccessStatusCode();

        var metrics = await response.Content.ReadFromJsonAsync<DashboardMetricsDto>(CustomWebApplicationFactory.JsonOptions);

        Assert.NotNull(metrics);
        Assert.True(metrics.EntriesLastHour >= 0);
        Assert.True(metrics.ExitsLastHour >= 0);
    }
}