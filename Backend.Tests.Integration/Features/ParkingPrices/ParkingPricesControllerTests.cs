using System.Net;
using System.Net.Http.Json;
using Backend.Database;
using Backend.Features.ParkingPrices;
using Backend.Features.Users;
using Microsoft.Extensions.DependencyInjection;
using tests.Setup;

namespace Backend.Tests.Integration.Features.ParkingPrices;

public class ParkingPricesControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();
    private readonly IServiceScopeFactory _scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public static TheoryData<string, string> WriteEndpoints()
        => new()
        {
            { "POST", "/api/parking-prices" },
            { "PATCH", "/api/parking-prices/11111111-1111-1111-1111-111111111111" },
            { "DELETE", "/api/parking-prices/11111111-1111-1111-1111-111111111111" }
        };

    [Theory]
    [MemberData(nameof(WriteEndpoints))]
    public async Task WriteEndpoints_WhenNotAuthenticated_ReturnUnauthorized(string method, string path)
    {
        var anonymousClient = AuthTestHelper.CreateAnonymousClient(_factory);

        var response = await SendWriteRequestAsync(anonymousClient, method, path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(WriteEndpoints))]
    public async Task WriteEndpoints_WhenCustomerAuthenticated_ReturnForbidden(string method, string path)
    {
        var customerClient = await AuthTestHelper.CreateClientAsAsync(_factory, UserRole.Customer);

        var response = await SendWriteRequestAsync(customerClient, method, path);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReadEndpoints_WhenAnonymous_AreAccessible()
    {
        var seeded = await SeedParkingPriceAsync();
        var anonymousClient = AuthTestHelper.CreateAnonymousClient(_factory);

        var listResponse = await anonymousClient.GetAsync("/api/parking-prices");
        var byIdResponse = await anonymousClient.GetAsync($"/api/parking-prices/{seeded.ParkingPriceId}");
        var currentResponse = await anonymousClient.GetAsync("/parking-pricing");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, byIdResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, currentResponse.StatusCode);
    }

    [Fact]
    public async Task ReadEndpoints_WhenCustomer_AreAccessible()
    {
        var seeded = await SeedParkingPriceAsync();
        var customerClient = await AuthTestHelper.CreateClientAsAsync(_factory, UserRole.Customer);

        var listResponse = await customerClient.GetAsync("/api/parking-prices");
        var byIdResponse = await customerClient.GetAsync($"/api/parking-prices/{seeded.ParkingPriceId}");
        var currentResponse = await customerClient.GetAsync("/parking-pricing");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, byIdResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, currentResponse.StatusCode);
    }

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
        var adminClient = await AuthTestHelper.CreateClientAsAsync(_factory, UserRole.Admin);

        var newPrice = new CreateParkingPriceDto
        {
            ToleranceMinutes = 15,
            BasePrice = 15.00m,
            HourlyRate = 5.00m,
            ThresholdMinutes = 180
        };

        var response = await adminClient.PostAsync("/api/parking-prices", JsonContent.Create(newPrice, options: CustomWebApplicationFactory.JsonOptions));

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
        var adminClient = await AuthTestHelper.CreateClientAsAsync(_factory, UserRole.Admin);

        var newPrice = new CreateParkingPriceDto
        {
            ToleranceMinutes = 20,
            BasePrice = 20.00m,
            HourlyRate = 6.00m,
            ThresholdMinutes = 240
        };

        var createResponse = await adminClient.PostAsync("/api/parking-prices", JsonContent.Create(newPrice, options: CustomWebApplicationFactory.JsonOptions));
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
        var adminClient = await AuthTestHelper.CreateClientAsAsync(_factory, UserRole.Admin);

        var newPrice = new CreateParkingPriceDto
        {
            ToleranceMinutes = 15,
            BasePrice = 15.00m,
            HourlyRate = 5.00m,
            ThresholdMinutes = 180
        };

        var createResponse = await adminClient.PostAsync("/api/parking-prices", JsonContent.Create(newPrice, options: CustomWebApplicationFactory.JsonOptions));
        createResponse.EnsureSuccessStatusCode();
        var createdPrice = await createResponse.Content.ReadFromJsonAsync<ParkingPricesDto>(CustomWebApplicationFactory.JsonOptions);

        var updateDto = new UpdateParkingPriceDto
        {
            BasePrice = 25.00m,
            HourlyRate = 7.00m
        };

        var updateResponse = await adminClient.PatchAsync($"/api/parking-prices/{createdPrice?.ParkingPriceId}", JsonContent.Create(updateDto, options: CustomWebApplicationFactory.JsonOptions));
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
        var adminClient = await AuthTestHelper.CreateClientAsAsync(_factory, UserRole.Admin);
        var invalidId = Guid.NewGuid();
        var updateDto = new UpdateParkingPriceDto { BasePrice = 25.00m };

        var response = await adminClient.PatchAsync($"/api/parking-prices/{invalidId}", JsonContent.Create(updateDto, options: CustomWebApplicationFactory.JsonOptions));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateParkingPrice_WithPartialData_ShouldUpdateOnlyProvidedFields()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(_factory, UserRole.Admin);

        var newPrice = new CreateParkingPriceDto
        {
            ToleranceMinutes = 15,
            BasePrice = 15.00m,
            HourlyRate = 5.00m,
            ThresholdMinutes = 180
        };

        var createResponse = await adminClient.PostAsync("/api/parking-prices", JsonContent.Create(newPrice, options: CustomWebApplicationFactory.JsonOptions));
        createResponse.EnsureSuccessStatusCode();
        var createdPrice = await createResponse.Content.ReadFromJsonAsync<ParkingPricesDto>(CustomWebApplicationFactory.JsonOptions);

        var updateDto = new UpdateParkingPriceDto { BasePrice = 30.00m };

        var updateResponse = await adminClient.PatchAsync($"/api/parking-prices/{createdPrice?.ParkingPriceId}", JsonContent.Create(updateDto, options: CustomWebApplicationFactory.JsonOptions));
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
        var adminClient = await AuthTestHelper.CreateClientAsAsync(_factory, UserRole.Admin);

        var newPrice = new CreateParkingPriceDto
        {
            ToleranceMinutes = 15,
            BasePrice = 15.00m,
            HourlyRate = 5.00m,
            ThresholdMinutes = 180
        };

        var createResponse = await adminClient.PostAsync("/api/parking-prices", JsonContent.Create(newPrice, options: CustomWebApplicationFactory.JsonOptions));
        createResponse.EnsureSuccessStatusCode();
        var createdPrice = await createResponse.Content.ReadFromJsonAsync<ParkingPricesDto>(CustomWebApplicationFactory.JsonOptions);

        var deleteResponse = await adminClient.DeleteAsync($"/api/parking-prices/{createdPrice?.ParkingPriceId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/parking-prices/{createdPrice?.ParkingPriceId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteParkingPrice_WithInvalidId_ShouldReturnNotFound()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(_factory, UserRole.Admin);
        var invalidId = Guid.NewGuid();

        var response = await adminClient.DeleteAsync($"/api/parking-prices/{invalidId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MultipleParkingPrices_ShouldBeListedCorrectly()
    {
        var adminClient = await AuthTestHelper.CreateClientAsAsync(_factory, UserRole.Admin);

        var prices = new[]
        {
            new CreateParkingPriceDto { ToleranceMinutes = 15, BasePrice = 15.00m, HourlyRate = 5.00m, ThresholdMinutes = 180 },
            new CreateParkingPriceDto { ToleranceMinutes = 20, BasePrice = 20.00m, HourlyRate = 6.00m, ThresholdMinutes = 240 },
            new CreateParkingPriceDto { ToleranceMinutes = 25, BasePrice = 25.00m, HourlyRate = 7.00m, ThresholdMinutes = 300 }
        };

        foreach (var price in prices)
        {
            var response = await adminClient.PostAsync("/api/parking-prices", JsonContent.Create(price, options: CustomWebApplicationFactory.JsonOptions));
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
        var adminClient = await AuthTestHelper.CreateClientAsAsync(_factory, UserRole.Admin);

        var newPrice1 = new CreateParkingPriceDto
        {
            ToleranceMinutes = 15,
            BasePrice = 15.00m,
            HourlyRate = 5.00m,
            ThresholdMinutes = 180
        };
        await adminClient.PostAsync("/api/parking-prices", JsonContent.Create(newPrice1, options: CustomWebApplicationFactory.JsonOptions));

        var newPrice2 = new CreateParkingPriceDto
        {
            ToleranceMinutes = 20,
            BasePrice = 20.00m,
            HourlyRate = 6.00m,
            ThresholdMinutes = 240
        };
        var createResponse2 = await adminClient.PostAsync("/api/parking-prices", JsonContent.Create(newPrice2, options: CustomWebApplicationFactory.JsonOptions));
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

    private async Task<HttpResponseMessage> SendWriteRequestAsync(HttpClient client, string method, string path)
    {
        return method switch
        {
            "POST" => await client.PostAsJsonAsync(path, new CreateParkingPriceDto
            {
                ToleranceMinutes = 15,
                BasePrice = 10m,
                HourlyRate = 5m,
                ThresholdMinutes = 120
            }, CustomWebApplicationFactory.JsonOptions),
            "PATCH" => await client.PatchAsync(path, JsonContent.Create(new UpdateParkingPriceDto
            {
                BasePrice = 20m
            }, options: CustomWebApplicationFactory.JsonOptions)),
            "DELETE" => await client.DeleteAsync(path),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, "Unsupported method")
        };
    }

    private async Task<ParkingPrice> SeedParkingPriceAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var entity = new ParkingPrice
        {
            ToleranceMinutes = 10,
            BasePrice = 12m,
            HourlyRate = 4m,
            ThresholdMinutes = 120
        };

        db.ParkingPrices.Add(entity);
        await db.SaveChangesAsync();

        return entity;
    }
}
