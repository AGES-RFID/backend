using System.Net;
using System.Net.Http.Json;
using Backend.Features.ParkingPrices;
using tests.Setup;

namespace Backend.Tests.Integration.Features.ParkingPrices;

public class ParkingPricesControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetAllParkingPrices_ShouldReturnSuccess()
    {
        var response = await _client.GetAsync("/api/parking-prices");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var prices = await response.Content.ReadFromJsonAsync<List<ParkingPricesDto>>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(prices);
    }

    [Fact]
    public async Task CreateParkingPrice_ShouldReturnCreatedPrice()
    {
        var newPrice = new CreateParkingPriceDto
        {
            ToleranceMinutes = 15,
            BasePrice = 15.00m,
            HourlyRate = 5.00m,
            ThresholdMinutes = 180
        };

        var response = await _client.PostAsync("/api/parking-prices", JsonContent.Create(newPrice, options: CustomWebApplicationFactory.JsonOptions));

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdPrice = await response.Content.ReadFromJsonAsync<ParkingPricesDto>(CustomWebApplicationFactory.JsonOptions);
        Assert.NotNull(createdPrice);
        Assert.Equal(newPrice.ToleranceMinutes, createdPrice.ToleranceMinutes);
        Assert.Equal(newPrice.BasePrice, createdPrice.BasePrice);
        Assert.Equal(newPrice.HourlyRate, createdPrice.HourlyRate);
        Assert.Equal(newPrice.ThresholdMinutes, createdPrice.ThresholdMinutes);
    }

    [Fact]
    public async Task GetParkingPrice_ShouldReturnCreatedPrice()
    {
        var newPrice = new CreateParkingPriceDto
        {
            ToleranceMinutes = 20,
            BasePrice = 20.00m,
            HourlyRate = 6.00m,
            ThresholdMinutes = 240
        };

        var createResponse = await _client.PostAsync("/api/parking-prices", JsonContent.Create(newPrice, options: CustomWebApplicationFactory.JsonOptions));
        createResponse.EnsureSuccessStatusCode();
        var createdPrice = await createResponse.Content.ReadFromJsonAsync<ParkingPricesDto>(CustomWebApplicationFactory.JsonOptions);

        var getResponse = await _client.GetAsync($"/api/parking-prices/{createdPrice?.ParkingPriceId}");
        getResponse.EnsureSuccessStatusCode();
        var fetchedPrice = await getResponse.Content.ReadFromJsonAsync<ParkingPricesDto>(CustomWebApplicationFactory.JsonOptions);

        Assert.NotNull(fetchedPrice);
        Assert.Equal(createdPrice?.ParkingPriceId, fetchedPrice.ParkingPriceId);
        Assert.Equal(createdPrice?.ToleranceMinutes, fetchedPrice.ToleranceMinutes);
        Assert.Equal(createdPrice?.BasePrice, fetchedPrice.BasePrice);
    }

    [Fact]
    public async Task GetParkingPrice_WithInvalidId_ShouldReturnNotFound()
    {
        var invalidId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/parking-prices/{invalidId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateParkingPrice_ShouldReturnUpdatedPrice()
    {
        var newPrice = new CreateParkingPriceDto
        {
            ToleranceMinutes = 15,
            BasePrice = 15.00m,
            HourlyRate = 5.00m,
            ThresholdMinutes = 180
        };

        var createResponse = await _client.PostAsync("/api/parking-prices", JsonContent.Create(newPrice, options: CustomWebApplicationFactory.JsonOptions));
        createResponse.EnsureSuccessStatusCode();
        var createdPrice = await createResponse.Content.ReadFromJsonAsync<ParkingPricesDto>(CustomWebApplicationFactory.JsonOptions);

        var updateDto = new UpdateParkingPriceDto
        {
            BasePrice = 25.00m,
            HourlyRate = 7.00m
        };

        var updateResponse = await _client.PatchAsync($"/api/parking-prices/{createdPrice?.ParkingPriceId}", JsonContent.Create(updateDto, options: CustomWebApplicationFactory.JsonOptions));
        updateResponse.EnsureSuccessStatusCode();
        var updatedPrice = await updateResponse.Content.ReadFromJsonAsync<ParkingPricesDto>(CustomWebApplicationFactory.JsonOptions);

        Assert.NotNull(updatedPrice);
        Assert.Equal(25.00m, updatedPrice.BasePrice);
        Assert.Equal(7.00m, updatedPrice.HourlyRate);
        Assert.Equal(createdPrice?.ToleranceMinutes, updatedPrice.ToleranceMinutes); // unchanged
    }

    [Fact]
    public async Task UpdateParkingPrice_WithInvalidId_ShouldReturnNotFound()
    {
        var invalidId = Guid.NewGuid();
        var updateDto = new UpdateParkingPriceDto { BasePrice = 25.00m };

        var response = await _client.PatchAsync($"/api/parking-prices/{invalidId}", JsonContent.Create(updateDto, options: CustomWebApplicationFactory.JsonOptions));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateParkingPrice_WithPartialData_ShouldUpdateOnlyProvidedFields()
    {
        var newPrice = new CreateParkingPriceDto
        {
            ToleranceMinutes = 15,
            BasePrice = 15.00m,
            HourlyRate = 5.00m,
            ThresholdMinutes = 180
        };

        var createResponse = await _client.PostAsync("/api/parking-prices", JsonContent.Create(newPrice, options: CustomWebApplicationFactory.JsonOptions));
        createResponse.EnsureSuccessStatusCode();
        var createdPrice = await createResponse.Content.ReadFromJsonAsync<ParkingPricesDto>(CustomWebApplicationFactory.JsonOptions);

        var updateDto = new UpdateParkingPriceDto { BasePrice = 30.00m };

        var updateResponse = await _client.PatchAsync($"/api/parking-prices/{createdPrice?.ParkingPriceId}", JsonContent.Create(updateDto, options: CustomWebApplicationFactory.JsonOptions));
        updateResponse.EnsureSuccessStatusCode();
        var updatedPrice = await updateResponse.Content.ReadFromJsonAsync<ParkingPricesDto>(CustomWebApplicationFactory.JsonOptions);

        Assert.NotNull(updatedPrice);
        Assert.Equal(30.00m, updatedPrice.BasePrice);
        Assert.Equal(createdPrice?.HourlyRate, updatedPrice.HourlyRate);
        Assert.Equal(createdPrice?.ToleranceMinutes, updatedPrice.ToleranceMinutes);
    }

    [Fact]
    public async Task DeleteParkingPrice_ShouldReturnNoContent()
    {
        var newPrice = new CreateParkingPriceDto
        {
            ToleranceMinutes = 15,
            BasePrice = 15.00m,
            HourlyRate = 5.00m,
            ThresholdMinutes = 180
        };

        var createResponse = await _client.PostAsync("/api/parking-prices", JsonContent.Create(newPrice, options: CustomWebApplicationFactory.JsonOptions));
        createResponse.EnsureSuccessStatusCode();
        var createdPrice = await createResponse.Content.ReadFromJsonAsync<ParkingPricesDto>(CustomWebApplicationFactory.JsonOptions);

        var deleteResponse = await _client.DeleteAsync($"/api/parking-prices/{createdPrice?.ParkingPriceId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/parking-prices/{createdPrice?.ParkingPriceId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteParkingPrice_WithInvalidId_ShouldReturnNotFound()
    {
        var invalidId = Guid.NewGuid();

        var response = await _client.DeleteAsync($"/api/parking-prices/{invalidId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MultipleParkingPrices_ShouldBeListedCorrectly()
    {
        var prices = new[]
        {
            new CreateParkingPriceDto { ToleranceMinutes = 15, BasePrice = 15.00m, HourlyRate = 5.00m, ThresholdMinutes = 180 },
            new CreateParkingPriceDto { ToleranceMinutes = 20, BasePrice = 20.00m, HourlyRate = 6.00m, ThresholdMinutes = 240 },
            new CreateParkingPriceDto { ToleranceMinutes = 25, BasePrice = 25.00m, HourlyRate = 7.00m, ThresholdMinutes = 300 }
        };

        foreach (var price in prices)
        {
            var response = await _client.PostAsync("/api/parking-prices", JsonContent.Create(price, options: CustomWebApplicationFactory.JsonOptions));
            response.EnsureSuccessStatusCode();
        }

        var getAllResponse = await _client.GetAsync("/api/parking-prices");
        getAllResponse.EnsureSuccessStatusCode();
        var allPrices = await getAllResponse.Content.ReadFromJsonAsync<List<ParkingPricesDto>>(CustomWebApplicationFactory.JsonOptions);

        Assert.NotNull(allPrices);
        Assert.True(allPrices.Count >= 3);
    }
    [Fact]
    public async Task GetCurrentParkingPricing_ShouldReturnLatestPrice()
    {
        var newPrice1 = new CreateParkingPriceDto
        {
            ToleranceMinutes = 15,
            BasePrice = 15.00m,
            HourlyRate = 5.00m,
            ThresholdMinutes = 180
        };
        
        await _client.PostAsync("/api/parking-prices", JsonContent.Create(newPrice1, options: CustomWebApplicationFactory.JsonOptions));

        var newPrice2 = new CreateParkingPriceDto
        {
            ToleranceMinutes = 20,
            BasePrice = 20.00m,
            HourlyRate = 6.00m,
            ThresholdMinutes = 240
        };
        
        var createResponse2 = await _client.PostAsync("/api/parking-prices", JsonContent.Create(newPrice2, options: CustomWebApplicationFactory.JsonOptions));
        var createdPrice2 = await createResponse2.Content.ReadFromJsonAsync<ParkingPricesDto>(CustomWebApplicationFactory.JsonOptions);

        var getResponse = await _client.GetAsync("/parking-pricing");
        getResponse.EnsureSuccessStatusCode();
        
        var currentPrice = await getResponse.Content.ReadFromJsonAsync<ParkingPricesDto>(CustomWebApplicationFactory.JsonOptions);

        Assert.NotNull(currentPrice);
        Assert.Equal(createdPrice2?.ParkingPriceId, currentPrice.ParkingPriceId);
        Assert.Equal(20.00m, currentPrice.BasePrice);
    }

    [Fact]
    public async Task GetCurrentParkingPricing_WhenNoPrices_ShouldReturnNotFound()
    {
        var response = await _client.GetAsync("/parking-pricing");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
