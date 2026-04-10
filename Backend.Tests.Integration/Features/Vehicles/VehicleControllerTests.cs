using System.Net;
using System.Net.Http.Json;
using Backend.Features.Users;
using Backend.Features.Vehicles;
using tests.Setup;

namespace tests.Features.Vehicles;

public class VehicleControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;


    //Test cases for Create
    [Fact]
    public async Task CreateVehicle_WithValidData_ShouldReturnCreated()
    {
        var userInfo = await _client.PostAsync("/api/users", JsonContent.Create(new CreateUserDto { Name = "Fulano", Email = "fulano@gmail.com" }));
        var user = await userInfo.Content.ReadFromJsonAsync<UserDto>();

        var dto = new CreateVehicleDto { Brand = "Honda", Model = "HRV", Plate = "AAA9A99", UserId = user!.UserId };
        var response = await _client.PostAsync("/api/vehicles", JsonContent.Create(dto));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<VehicleDto>();
        Assert.NotNull(created);
        Assert.Equal(dto.Plate, created.Plate);
        Assert.Equal(dto.Brand, created.Brand);
        Assert.Equal(dto.Model, created.Model);
        Assert.Equal(dto.UserId, created.UserId);
    }

    [Fact]
    public async Task CreateVehicle_WithInvalidPayload_ShouldReturnBadRequest()
    {
        var response = await _client.PostAsync("/api/vehicles", JsonContent.Create(new { Brand = "Missing Required Info" }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateVehicle_WithNonExistentUserId_ShouldReturnNotFound()
    {
        var dto = new CreateVehicleDto { Brand = "Honda", Model = "HRV", Plate = "AAA9A99", UserId = Guid.NewGuid() };
        var response = await _client.PostAsync("/api/vehicles", JsonContent.Create(dto));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateVehicle_WithExistingPlate_ShouldReturnConflict()
    {
        var userInfo = await _client.PostAsync("/api/users", JsonContent.Create(new CreateUserDto { Name = "Fulaninho", Email = "fulano@gmail.com" }));
        var user = await userInfo.Content.ReadFromJsonAsync<UserDto>();

        var dto = new CreateVehicleDto { Brand = "Honda", Model = "HRV", Plate = "SAMEPLT", UserId = user!.UserId };
        await _client.PostAsync("/api/vehicles", JsonContent.Create(dto));

        var response = await _client.PostAsync("/api/vehicles", JsonContent.Create(dto));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
    //Teste case for Upate
    [Fact]
    public async Task UpdateVehicle_WithValidData_ShouldReturnOk()
    {
        var userInfo = await _client.PostAsync("/api/users", JsonContent.Create(new CreateUserDto { Name = "Fulaninho", Email = "Fulano@gmail.com" }));
        var user = await userInfo.Content.ReadFromJsonAsync<UserDto>();

        var dto = new CreateVehicleDto { Brand = "Honda", Model = "HRV", Plate = "UPDT001", UserId = user!.UserId };
        var createResponse = await _client.PostAsync("/api/vehicles", JsonContent.Create(dto));
        var created = await createResponse.Content.ReadFromJsonAsync<VehicleDto>();

        var updateDto = new CreateVehicleDto { Brand = "Toyota", Model = "Corolla", Plate = "UPDT002", UserId = user.UserId };
        var updateResponse = await _client.PutAsync($"/api/vehicles/{created!.VehicleId}", JsonContent.Create(updateDto));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<VehicleDto>();
        Assert.Equal("Toyota", updated!.Brand);
        Assert.Equal("UPDT002", updated.Plate);
    }

    [Fact]
    public async Task UpdateVehicle_WithInvalidPayload_ShouldReturnBadRequest()
    {
        var response = await _client.PutAsync($"/api/vehicles/{Guid.NewGuid()}", JsonContent.Create(new { MissingRequiredBody = true }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateVehicle_WithNonExistentVehicleId_ShouldReturnNotFound()
    {
        var updateDto = new CreateVehicleDto { Brand = "Toyota", Model = "Corolla", Plate = "BBB0B00", UserId = Guid.NewGuid() };
        var response = await _client.PutAsync($"/api/vehicles/{Guid.NewGuid()}", JsonContent.Create(updateDto));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateVehicle_WithPlateOfAnotherVehicle_ShouldReturnConflict()
    {
        var userInfo = await _client.PostAsync("/api/users", JsonContent.Create(new CreateUserDto { Name = "Fulaninho", Email = "fulano@gmail.com" }));
        var user = await userInfo.Content.ReadFromJsonAsync<UserDto>();

        var dto1 = new CreateVehicleDto { Brand = "Honda", Model = "HRV", Plate = "PLATE1", UserId = user!.UserId };
        var dto2 = new CreateVehicleDto { Brand = "Ford", Model = "Ka", Plate = "PLATE2", UserId = user.UserId };

        await _client.PostAsync("/api/vehicles", JsonContent.Create(dto1));
        var res2 = await _client.PostAsync("/api/vehicles", JsonContent.Create(dto2));
        var created2 = await res2.Content.ReadFromJsonAsync<VehicleDto>();

        var updateDto = new CreateVehicleDto { Brand = "Ford", Model = "Ka", Plate = "PLATE1", UserId = user.UserId };
        var updateResponse = await _client.PutAsync($"/api/vehicles/{created2!.VehicleId}", JsonContent.Create(updateDto));

        Assert.Equal(HttpStatusCode.Conflict, updateResponse.StatusCode);
    }

    //Test case para GETS
    [Fact]
    public async Task GetVehicles_ShouldReturnOkWithList()
    {
        var response = await _client.GetAsync("/api/vehicles");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<VehicleDto>>();
        Assert.NotNull(list);
    }

    [Fact]
    public async Task GetVehicleById_WithExistingId_ShouldReturnOk()
    {
        var userInfo = await _client.PostAsync("/api/users", JsonContent.Create(new CreateUserDto { Name = "Owner", Email = "g@g.com" }));
        var user = await userInfo.Content.ReadFromJsonAsync<UserDto>();
        var dto = new CreateVehicleDto { Brand = "Honda", Model = "HRV", Plate = "GETID01", UserId = user!.UserId };
        var createResponse = await _client.PostAsync("/api/vehicles", JsonContent.Create(dto));
        var created = await createResponse.Content.ReadFromJsonAsync<VehicleDto>();

        var response = await _client.GetAsync($"/api/vehicles/{created!.VehicleId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await response.Content.ReadFromJsonAsync<VehicleDto>();
        Assert.NotNull(fetched);
        Assert.Equal(created.VehicleId, fetched!.VehicleId);
    }

    [Fact]
    public async Task GetVehicleById_WithNonExistentId_ShouldReturnNotFound()
    {
        var response = await _client.GetAsync($"/api/vehicles/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    //Test cases para DELETE
    [Fact]
    public async Task DeleteVehicle_WithValidId_ShouldReturnNoContent()
    {
        var userInfo = await _client.PostAsync("/api/users", JsonContent.Create(new CreateUserDto { Name = "Owner", Email = "f@f.com" }));
        var user = await userInfo.Content.ReadFromJsonAsync<UserDto>();
        var dto = new CreateVehicleDto { Brand = "Honda", Model = "HRV", Plate = "DELT001", UserId = user!.UserId };
        var createResponse = await _client.PostAsync("/api/vehicles", JsonContent.Create(dto));
        var created = await createResponse.Content.ReadFromJsonAsync<VehicleDto>();

        var delResponse = await _client.DeleteAsync($"/api/vehicles/{created!.VehicleId}");
        Assert.Equal(HttpStatusCode.NoContent, delResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteVehicle_WithNonExistentId_ShouldReturnNotFound()
    {
        var response = await _client.DeleteAsync($"/api/vehicles/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}