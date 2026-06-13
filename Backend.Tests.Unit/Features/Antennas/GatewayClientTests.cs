using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Backend.Features.Antennas;

namespace Backend.Tests.Unit.Features.Antennas;

public class GatewayClientTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static HttpClient CreateClient(HttpStatusCode status, object? body = null)
    {
        var json = body is null ? "null" : JsonSerializer.Serialize(body, JsonOptions);
        var handler = new StubHttpHandler(status, json);
        return new HttpClient(handler) { BaseAddress = new Uri("http://gateway/") };
    }

    // --- GetAntennasAsync ---

    [Fact]
    public async Task GetAntennasAsync_WhenSuccess_ReturnsDeserializedList()
    {
        var expected = new List<AntennaDto>
        {
            new() { Id = Guid.NewGuid(), Number = 1, Status = "On", Sensibility = 80, Power = 30 }
        };
        var client = new GatewayClient(CreateClient(HttpStatusCode.OK, expected));

        var result = await client.GetAntennasAsync();

        Assert.Single(result);
        Assert.Equal(expected[0].Number, result[0].Number);
        Assert.Equal(expected[0].Status, result[0].Status);
    }

    [Fact]
    public async Task GetAntennasAsync_WhenNullBody_ReturnsEmptyList()
    {
        var client = new GatewayClient(CreateClient(HttpStatusCode.OK, null));

        var result = await client.GetAntennasAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAntennasAsync_WhenNonSuccess_ThrowsGatewayException()
    {
        var client = new GatewayClient(CreateClient(HttpStatusCode.ServiceUnavailable));

        var ex = await Assert.ThrowsAsync<GatewayException>(() => client.GetAntennasAsync());

        Assert.Equal(503, ex.StatusCode);
    }

    // --- GetAntennaAsync ---

    [Fact]
    public async Task GetAntennaAsync_WhenSuccess_ReturnsDeserializedAntenna()
    {
        var id = Guid.NewGuid();
        var expected = new AntennaDto { Id = id, Number = 2, Status = "Off" };
        var client = new GatewayClient(CreateClient(HttpStatusCode.OK, expected));

        var result = await client.GetAntennaAsync(id);

        Assert.Equal(expected.Id, result.Id);
        Assert.Equal(expected.Status, result.Status);
    }

    [Fact]
    public async Task GetAntennaAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        var client = new GatewayClient(CreateClient(HttpStatusCode.NotFound));

        await Assert.ThrowsAsync<KeyNotFoundException>(() => client.GetAntennaAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetAntennaAsync_WhenNonSuccessNotNotFound_ThrowsGatewayException()
    {
        var client = new GatewayClient(CreateClient(HttpStatusCode.BadGateway));

        var ex = await Assert.ThrowsAsync<GatewayException>(() => client.GetAntennaAsync(Guid.NewGuid()));

        Assert.Equal(502, ex.StatusCode);
    }

    [Fact]
    public async Task GetAntennaAsync_WhenNullBody_ThrowsGatewayException()
    {
        var client = new GatewayClient(CreateClient(HttpStatusCode.OK, null));

        await Assert.ThrowsAsync<GatewayException>(() => client.GetAntennaAsync(Guid.NewGuid()));
    }

    // --- UpdateAntennaAsync ---

    [Fact]
    public async Task UpdateAntennaAsync_WhenSuccess_ReturnsDeserializedAntenna()
    {
        var id = Guid.NewGuid();
        var expected = new AntennaDto { Id = id, Number = 1, Status = "On", Sensibility = 60, Power = 20 };
        var client = new GatewayClient(CreateClient(HttpStatusCode.OK, expected));

        var result = await client.UpdateAntennaAsync(id, new UpdateAntennaDto { Status = "On" });

        Assert.Equal(expected.Id, result.Id);
        Assert.Equal(expected.Status, result.Status);
    }

    [Fact]
    public async Task UpdateAntennaAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        var client = new GatewayClient(CreateClient(HttpStatusCode.NotFound));

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            client.UpdateAntennaAsync(Guid.NewGuid(), new UpdateAntennaDto()));
    }

    [Fact]
    public async Task UpdateAntennaAsync_WhenNonSuccessNotNotFound_ThrowsGatewayException()
    {
        var client = new GatewayClient(CreateClient(HttpStatusCode.InternalServerError));

        var ex = await Assert.ThrowsAsync<GatewayException>(() =>
            client.UpdateAntennaAsync(Guid.NewGuid(), new UpdateAntennaDto()));

        Assert.Equal(500, ex.StatusCode);
    }

    [Fact]
    public async Task UpdateAntennaAsync_WhenNullBody_ThrowsGatewayException()
    {
        var client = new GatewayClient(CreateClient(HttpStatusCode.OK, null));

        await Assert.ThrowsAsync<GatewayException>(() =>
            client.UpdateAntennaAsync(Guid.NewGuid(), new UpdateAntennaDto()));
    }

    private sealed class StubHttpHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
