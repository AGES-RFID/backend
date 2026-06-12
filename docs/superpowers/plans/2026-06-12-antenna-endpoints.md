# Antenna Endpoints Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add three admin-only antenna endpoints (`GET /api/antennas`, `GET /api/antennas/{id}`, `PUT /api/antennas/{id}`) that proxy to an external gateway — no DB storage.

**Architecture:** A typed HTTP client (`IGatewayClient`) is injected into `AntennaService`, which validates inputs and delegates to the gateway. `AntennaController` maps exceptions to HTTP status codes. The pattern mirrors the existing Controller → Interface/Service layout throughout the project.

**Tech Stack:** .NET 10, ASP.NET Core, xUnit, NSubstitute, Testcontainers (integration tests already set up)

---

## File Map

| Action | Path |
|--------|------|
| Create | `Backend.API/Features/Antennas/Dtos/AntennaDto.cs` |
| Create | `Backend.API/Features/Antennas/Dtos/UpdateAntennaDto.cs` |
| Create | `Backend.API/Features/Antennas/GatewayClient.cs` — `GatewayException`, `IGatewayClient`, `GatewayClient` |
| Create | `Backend.API/Features/Antennas/AntennaService.cs` — `IAntennaService`, `AntennaService` |
| Create | `Backend.API/Features/Antennas/AntennaController.cs` |
| Modify | `Backend.API/appsettings.json` — add `Gateway.BaseUrl` |
| Modify | `Backend.API/Program.cs` — register typed client + service |
| Create | `Backend.Tests.Unit/Features/Antennas/AntennaControllerTests.cs` |
| Create | `Backend.Tests.Unit/Features/Antennas/AntennaServiceTests.cs` |
| Create | `Backend.Tests.Integration/Features/Antennas/AntennaControllerTests.cs` |

---

## Task 1: Create a feature branch

- [ ] **Step 1: Create and switch to the feature branch**

```bash
git checkout -b feat/83-antenna-endpoints
```

Expected: switched to new branch `feat/83-antenna-endpoints`

---

## Task 2: DTOs

**Files:**
- Create: `Backend.API/Features/Antennas/Dtos/AntennaDto.cs`
- Create: `Backend.API/Features/Antennas/Dtos/UpdateAntennaDto.cs`

- [ ] **Step 1: Create `AntennaDto.cs`**

```csharp
namespace Backend.Features.Antennas;

public class AntennaDto
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? Sensibility { get; set; }
    public int? Power { get; set; }
}
```

- [ ] **Step 2: Create `UpdateAntennaDto.cs`**

```csharp
namespace Backend.Features.Antennas;

public class UpdateAntennaDto
{
    public string? Status { get; set; }
    public int? Sensibility { get; set; }
    public int? Power { get; set; }
}
```

- [ ] **Step 3: Commit**

```bash
git add Backend.API/Features/Antennas/Dtos/
git commit -m "feat(antennas): add DTOs"
```

---

## Task 3: Gateway infrastructure

**Files:**
- Create: `Backend.API/Features/Antennas/GatewayClient.cs`

- [ ] **Step 1: Create `GatewayClient.cs`** with exception, interface, and implementation

```csharp
using System.Net.Http.Json;

namespace Backend.Features.Antennas;

public class GatewayException(int statusCode) : Exception($"Gateway returned status {statusCode}")
{
    public int StatusCode { get; } = statusCode;
}

public interface IGatewayClient
{
    Task<List<AntennaDto>> GetAntennasAsync();
    Task<AntennaDto> GetAntennaAsync(Guid id);
    Task<AntennaDto> UpdateAntennaAsync(Guid id, UpdateAntennaDto dto);
}

public class GatewayClient(HttpClient httpClient) : IGatewayClient
{
    public async Task<List<AntennaDto>> GetAntennasAsync()
    {
        var response = await httpClient.GetAsync("antennas");
        if (!response.IsSuccessStatusCode)
            throw new GatewayException((int)response.StatusCode);
        return await response.Content.ReadFromJsonAsync<List<AntennaDto>>() ?? [];
    }

    public async Task<AntennaDto> GetAntennaAsync(Guid id)
    {
        var response = await httpClient.GetAsync($"antennas/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new KeyNotFoundException($"Antenna {id} not found");
        if (!response.IsSuccessStatusCode)
            throw new GatewayException((int)response.StatusCode);
        return await response.Content.ReadFromJsonAsync<AntennaDto>()
            ?? throw new GatewayException(500);
    }

    public async Task<AntennaDto> UpdateAntennaAsync(Guid id, UpdateAntennaDto dto)
    {
        var response = await httpClient.PutAsJsonAsync($"antennas/{id}", dto);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new KeyNotFoundException($"Antenna {id} not found");
        if (!response.IsSuccessStatusCode)
            throw new GatewayException((int)response.StatusCode);
        return await response.Content.ReadFromJsonAsync<AntennaDto>()
            ?? throw new GatewayException(500);
    }
}
```

- [ ] **Step 2: Verify it compiles**

```bash
dotnet build Backend.API
```

Expected: Build succeeded, 0 errors

- [ ] **Step 3: Commit**

```bash
git add Backend.API/Features/Antennas/GatewayClient.cs
git commit -m "feat(antennas): add gateway client"
```

---

## Task 4: AntennaService (TDD)

**Files:**
- Create: `Backend.API/Features/Antennas/AntennaService.cs`
- Create: `Backend.Tests.Unit/Features/Antennas/AntennaServiceTests.cs`

- [ ] **Step 1: Create the `IAntennaService` interface and skeleton `AntennaService`**

Create `Backend.API/Features/Antennas/AntennaService.cs`:

```csharp
namespace Backend.Features.Antennas;

public interface IAntennaService
{
    Task<List<AntennaDto>> GetAntennasAsync();
    Task<AntennaDto> GetAntennaAsync(Guid id);
    Task<AntennaDto> UpdateAntennaAsync(Guid id, UpdateAntennaDto dto);
}

public class AntennaService(IGatewayClient gatewayClient) : IAntennaService
{
    public Task<List<AntennaDto>> GetAntennasAsync() =>
        throw new NotImplementedException();

    public Task<AntennaDto> GetAntennaAsync(Guid id) =>
        throw new NotImplementedException();

    public Task<AntennaDto> UpdateAntennaAsync(Guid id, UpdateAntennaDto dto) =>
        throw new NotImplementedException();
}
```

- [ ] **Step 2: Write unit tests for `AntennaService`**

Create `Backend.Tests.Unit/Features/Antennas/AntennaServiceTests.cs`:

```csharp
using Backend.Features.Antennas;
using NSubstitute;

namespace Backend.Tests.Unit.Features.Antennas;

public class AntennaServiceTests
{
    // --- GetAntennasAsync ---

    [Fact]
    public async Task GetAntennasAsync_DelegatesToGatewayClient()
    {
        var expected = new List<AntennaDto>
        {
            new() { Id = Guid.NewGuid(), Number = 1, Status = "On", Sensibility = 80, Power = 30 }
        };
        var client = Substitute.For<IGatewayClient>();
        client.GetAntennasAsync().Returns(expected);
        var service = new AntennaService(client);

        var result = await service.GetAntennasAsync();

        Assert.Equal(expected, result);
        await client.Received(1).GetAntennasAsync();
    }

    // --- GetAntennaAsync ---

    [Fact]
    public async Task GetAntennaAsync_DelegatesToGatewayClient()
    {
        var id = Guid.NewGuid();
        var expected = new AntennaDto { Id = id, Number = 1, Status = "On" };
        var client = Substitute.For<IGatewayClient>();
        client.GetAntennaAsync(id).Returns(expected);
        var service = new AntennaService(client);

        var result = await service.GetAntennaAsync(id);

        Assert.Equal(expected, result);
        await client.Received(1).GetAntennaAsync(id);
    }

    // --- UpdateAntennaAsync ---

    [Fact]
    public async Task UpdateAntennaAsync_WhenStatusIsNull_DefaultsToOff()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateAntennaDto { Status = null, Sensibility = 50, Power = 40 };
        var expected = new AntennaDto { Id = id, Number = 1, Status = "Off" };
        var client = Substitute.For<IGatewayClient>();
        client.UpdateAntennaAsync(id, Arg.Is<UpdateAntennaDto>(d => d.Status == "Off")).Returns(expected);
        var service = new AntennaService(client);

        await service.UpdateAntennaAsync(id, dto);

        await client.Received(1).UpdateAntennaAsync(id, Arg.Is<UpdateAntennaDto>(d => d.Status == "Off"));
    }

    [Fact]
    public async Task UpdateAntennaAsync_WhenSensibilityAbove100_ThrowsArgumentOutOfRange()
    {
        var client = Substitute.For<IGatewayClient>();
        var service = new AntennaService(client);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.UpdateAntennaAsync(Guid.NewGuid(), new UpdateAntennaDto { Sensibility = 101 }));

        await client.DidNotReceiveWithAnyArgs().UpdateAntennaAsync(default, default!);
    }

    [Fact]
    public async Task UpdateAntennaAsync_WhenSensibilityBelow0_ThrowsArgumentOutOfRange()
    {
        var client = Substitute.For<IGatewayClient>();
        var service = new AntennaService(client);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.UpdateAntennaAsync(Guid.NewGuid(), new UpdateAntennaDto { Sensibility = -1 }));

        await client.DidNotReceiveWithAnyArgs().UpdateAntennaAsync(default, default!);
    }

    [Fact]
    public async Task UpdateAntennaAsync_WhenPowerAbove100_ThrowsArgumentOutOfRange()
    {
        var client = Substitute.For<IGatewayClient>();
        var service = new AntennaService(client);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.UpdateAntennaAsync(Guid.NewGuid(), new UpdateAntennaDto { Power = 101 }));

        await client.DidNotReceiveWithAnyArgs().UpdateAntennaAsync(default, default!);
    }

    [Fact]
    public async Task UpdateAntennaAsync_WhenPowerBelow0_ThrowsArgumentOutOfRange()
    {
        var client = Substitute.For<IGatewayClient>();
        var service = new AntennaService(client);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.UpdateAntennaAsync(Guid.NewGuid(), new UpdateAntennaDto { Power = -1 }));

        await client.DidNotReceiveWithAnyArgs().UpdateAntennaAsync(default, default!);
    }

    [Fact]
    public async Task UpdateAntennaAsync_WhenValidInput_DelegatesToGatewayClient()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateAntennaDto { Status = "On", Sensibility = 80, Power = 30 };
        var expected = new AntennaDto { Id = id, Number = 1, Status = "On", Sensibility = 80, Power = 30 };
        var client = Substitute.For<IGatewayClient>();
        client.UpdateAntennaAsync(id, dto).Returns(expected);
        var service = new AntennaService(client);

        var result = await service.UpdateAntennaAsync(id, dto);

        Assert.Equal(expected, result);
        await client.Received(1).UpdateAntennaAsync(id, dto);
    }
}
```

- [ ] **Step 3: Run tests — expect failures**

```bash
dotnet test Backend.Tests.Unit --filter "FullyQualifiedName~AntennaServiceTests" -v minimal
```

Expected: multiple failures with `NotImplementedException`

- [ ] **Step 4: Implement `AntennaService`**

Replace the three `NotImplementedException` methods in `Backend.API/Features/Antennas/AntennaService.cs`:

```csharp
namespace Backend.Features.Antennas;

public interface IAntennaService
{
    Task<List<AntennaDto>> GetAntennasAsync();
    Task<AntennaDto> GetAntennaAsync(Guid id);
    Task<AntennaDto> UpdateAntennaAsync(Guid id, UpdateAntennaDto dto);
}

public class AntennaService(IGatewayClient gatewayClient) : IAntennaService
{
    public Task<List<AntennaDto>> GetAntennasAsync() =>
        gatewayClient.GetAntennasAsync();

    public Task<AntennaDto> GetAntennaAsync(Guid id) =>
        gatewayClient.GetAntennaAsync(id);

    public async Task<AntennaDto> UpdateAntennaAsync(Guid id, UpdateAntennaDto dto)
    {
        dto.Status ??= "Off";

        if (dto.Sensibility.HasValue && (dto.Sensibility < 0 || dto.Sensibility > 100))
            throw new ArgumentOutOfRangeException(nameof(dto.Sensibility), "Sensibility must be between 0 and 100");

        if (dto.Power.HasValue && (dto.Power < 0 || dto.Power > 100))
            throw new ArgumentOutOfRangeException(nameof(dto.Power), "Power must be between 0 and 100");

        return await gatewayClient.UpdateAntennaAsync(id, dto);
    }
}
```

- [ ] **Step 5: Run tests — expect all pass**

```bash
dotnet test Backend.Tests.Unit --filter "FullyQualifiedName~AntennaServiceTests" -v minimal
```

Expected: all tests PASS

- [ ] **Step 6: Commit**

```bash
git add Backend.API/Features/Antennas/AntennaService.cs Backend.Tests.Unit/Features/Antennas/AntennaServiceTests.cs
git commit -m "feat(antennas): add AntennaService with validation"
```

---

## Task 5: AntennaController (TDD)

**Files:**
- Create: `Backend.API/Features/Antennas/AntennaController.cs`
- Create: `Backend.Tests.Unit/Features/Antennas/AntennaControllerTests.cs`

- [ ] **Step 1: Create skeleton `AntennaController`**

Create `Backend.API/Features/Antennas/AntennaController.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.Antennas;

[ApiController]
[Route("api/antennas")]
[Authorize(Roles = "Admin")]
public class AntennaController(IAntennaService antennaService) : ControllerBase
{
    private readonly IAntennaService _antennaService = antennaService;

    [HttpGet]
    public Task<ActionResult<List<AntennaDto>>> GetAntennas() =>
        throw new NotImplementedException();

    [HttpGet("{id}")]
    public Task<ActionResult<AntennaDto>> GetAntenna(Guid id) =>
        throw new NotImplementedException();

    [HttpPut("{id}")]
    public Task<ActionResult<AntennaDto>> UpdateAntenna(Guid id, UpdateAntennaDto dto) =>
        throw new NotImplementedException();
}
```

- [ ] **Step 2: Write unit tests for `AntennaController`**

Create `Backend.Tests.Unit/Features/Antennas/AntennaControllerTests.cs`:

```csharp
using Backend.Features.Antennas;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Backend.Tests.Unit.Features.Antennas;

public class AntennaControllerTests
{
    // --- GET /api/antennas ---

    [Fact]
    public async Task GetAntennas_WhenServiceReturnsAntennas_ReturnsOkWithList()
    {
        var expected = new List<AntennaDto>
        {
            new() { Id = Guid.NewGuid(), Number = 1, Status = "On", Sensibility = 80, Power = 30 }
        };
        var service = Substitute.For<IAntennaService>();
        service.GetAntennasAsync().Returns(expected);
        var controller = new AntennaController(service);

        var result = await controller.GetAntennas();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, ok.Value);
        await service.Received(1).GetAntennasAsync();
    }

    [Fact]
    public async Task GetAntennas_WhenGatewayThrows_Returns502()
    {
        var service = Substitute.For<IAntennaService>();
        service.GetAntennasAsync().ThrowsAsync(new GatewayException(503));
        var controller = new AntennaController(service);

        var result = await controller.GetAntennas();

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(502, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetAntennas_WhenUnexpectedExceptionThrown_Returns500()
    {
        var service = Substitute.For<IAntennaService>();
        service.GetAntennasAsync().ThrowsAsync(new Exception("boom"));
        var controller = new AntennaController(service);

        var result = await controller.GetAntennas();

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    // --- GET /api/antennas/{id} ---

    [Fact]
    public async Task GetAntenna_WhenServiceReturnsAntenna_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var expected = new AntennaDto { Id = id, Number = 1, Status = "On" };
        var service = Substitute.For<IAntennaService>();
        service.GetAntennaAsync(id).Returns(expected);
        var controller = new AntennaController(service);

        var result = await controller.GetAntenna(id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task GetAntenna_WhenNotFound_Returns404()
    {
        var id = Guid.NewGuid();
        var service = Substitute.For<IAntennaService>();
        service.GetAntennaAsync(id).ThrowsAsync(new KeyNotFoundException());
        var controller = new AntennaController(service);

        var result = await controller.GetAntenna(id);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetAntenna_WhenGatewayThrows_Returns502()
    {
        var service = Substitute.For<IAntennaService>();
        service.GetAntennaAsync(Arg.Any<Guid>()).ThrowsAsync(new GatewayException(500));
        var controller = new AntennaController(service);

        var result = await controller.GetAntenna(Guid.NewGuid());

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(502, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetAntenna_WhenUnexpectedExceptionThrown_Returns500()
    {
        var service = Substitute.For<IAntennaService>();
        service.GetAntennaAsync(Arg.Any<Guid>()).ThrowsAsync(new Exception("boom"));
        var controller = new AntennaController(service);

        var result = await controller.GetAntenna(Guid.NewGuid());

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    // --- PUT /api/antennas/{id} ---

    [Fact]
    public async Task UpdateAntenna_WhenServiceReturnsAntenna_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateAntennaDto { Status = "On", Sensibility = 80, Power = 30 };
        var expected = new AntennaDto { Id = id, Number = 1, Status = "On", Sensibility = 80, Power = 30 };
        var service = Substitute.For<IAntennaService>();
        service.UpdateAntennaAsync(id, dto).Returns(expected);
        var controller = new AntennaController(service);

        var result = await controller.UpdateAntenna(id, dto);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task UpdateAntenna_WhenValidationFails_Returns400()
    {
        var service = Substitute.For<IAntennaService>();
        service.UpdateAntennaAsync(Arg.Any<Guid>(), Arg.Any<UpdateAntennaDto>())
            .ThrowsAsync(new ArgumentOutOfRangeException("sensibility", "Sensibility must be between 0 and 100"));
        var controller = new AntennaController(service);

        var result = await controller.UpdateAntenna(Guid.NewGuid(), new UpdateAntennaDto { Sensibility = 101 });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateAntenna_WhenNotFound_Returns404()
    {
        var service = Substitute.For<IAntennaService>();
        service.UpdateAntennaAsync(Arg.Any<Guid>(), Arg.Any<UpdateAntennaDto>())
            .ThrowsAsync(new KeyNotFoundException());
        var controller = new AntennaController(service);

        var result = await controller.UpdateAntenna(Guid.NewGuid(), new UpdateAntennaDto());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdateAntenna_WhenGatewayThrows_Returns502()
    {
        var service = Substitute.For<IAntennaService>();
        service.UpdateAntennaAsync(Arg.Any<Guid>(), Arg.Any<UpdateAntennaDto>())
            .ThrowsAsync(new GatewayException(503));
        var controller = new AntennaController(service);

        var result = await controller.UpdateAntenna(Guid.NewGuid(), new UpdateAntennaDto());

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(502, statusResult.StatusCode);
    }

    [Fact]
    public async Task UpdateAntenna_WhenUnexpectedExceptionThrown_Returns500()
    {
        var service = Substitute.For<IAntennaService>();
        service.UpdateAntennaAsync(Arg.Any<Guid>(), Arg.Any<UpdateAntennaDto>())
            .ThrowsAsync(new Exception("boom"));
        var controller = new AntennaController(service);

        var result = await controller.UpdateAntenna(Guid.NewGuid(), new UpdateAntennaDto());

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }
}
```

- [ ] **Step 3: Run tests — expect failures**

```bash
dotnet test Backend.Tests.Unit --filter "FullyQualifiedName~AntennaControllerTests" -v minimal
```

Expected: failures with `NotImplementedException`

- [ ] **Step 4: Implement `AntennaController`**

Replace the skeleton in `Backend.API/Features/Antennas/AntennaController.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.Antennas;

[ApiController]
[Route("api/antennas")]
[Authorize(Roles = "Admin")]
public class AntennaController(IAntennaService antennaService) : ControllerBase
{
    private readonly IAntennaService _antennaService = antennaService;

    [HttpGet]
    public async Task<ActionResult<List<AntennaDto>>> GetAntennas()
    {
        try
        {
            var antennas = await _antennaService.GetAntennasAsync();
            return Ok(antennas);
        }
        catch (GatewayException ex)
        {
            return StatusCode(502, new { error = $"Gateway error: {ex.StatusCode}" });
        }
        catch (Exception)
        {
            return Problem();
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AntennaDto>> GetAntenna(Guid id)
    {
        try
        {
            var antenna = await _antennaService.GetAntennaAsync(id);
            return Ok(antenna);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (GatewayException ex)
        {
            return StatusCode(502, new { error = $"Gateway error: {ex.StatusCode}" });
        }
        catch (Exception)
        {
            return Problem();
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AntennaDto>> UpdateAntenna(Guid id, UpdateAntennaDto dto)
    {
        try
        {
            var antenna = await _antennaService.UpdateAntennaAsync(id, dto);
            return Ok(antenna);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (GatewayException ex)
        {
            return StatusCode(502, new { error = $"Gateway error: {ex.StatusCode}" });
        }
        catch (Exception)
        {
            return Problem();
        }
    }
}
```

- [ ] **Step 5: Run tests — expect all pass**

```bash
dotnet test Backend.Tests.Unit --filter "FullyQualifiedName~AntennaControllerTests" -v minimal
```

Expected: all tests PASS

- [ ] **Step 6: Commit**

```bash
git add Backend.API/Features/Antennas/AntennaController.cs Backend.Tests.Unit/Features/Antennas/AntennaControllerTests.cs
git commit -m "feat(antennas): add AntennaController"
```

---

## Task 6: Wire up in Program.cs and appsettings

**Files:**
- Modify: `Backend.API/appsettings.json`
- Modify: `Backend.API/Program.cs`

- [ ] **Step 1: Add `Gateway` section to `appsettings.json`**

In `Backend.API/appsettings.json`, add the following key at the top level (alongside `ConnectionStrings`):

```json
"Gateway": {
  "BaseUrl": "http://localhost:8080"
}
```

- [ ] **Step 2: Register the typed client and service in `Program.cs`**

Add the following import at the top of `Backend.API/Program.cs`:

```csharp
using Backend.Features.Antennas;
```

Then add these two lines in the "Register feature services" section (after the existing `AddScoped` calls):

```csharp
builder.Services.AddHttpClient<IGatewayClient, GatewayClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Gateway:BaseUrl"]
        ?? throw new InvalidOperationException("Gateway:BaseUrl is required"));
});
builder.Services.AddScoped<IAntennaService, AntennaService>();
```

- [ ] **Step 3: Verify build**

```bash
dotnet build Backend.API
```

Expected: Build succeeded, 0 errors

- [ ] **Step 4: Commit**

```bash
git add Backend.API/appsettings.json Backend.API/Program.cs
git commit -m "feat(antennas): register gateway client and antenna service"
```

---

## Task 7: Integration tests (auth validation)

**Files:**
- Create: `Backend.Tests.Integration/Features/Antennas/AntennaControllerTests.cs`

- [ ] **Step 1: Expose `CreateTokenForUser` in `AuthTestHelper`**

In `Backend.Tests.Integration/Setup/AuthTestHelper.cs`, add this public method alongside `CreateAnonymousClient`:

```csharp
public static string CreateTokenForUser(User user) => CreateJwtToken(user);
```

- [ ] **Step 2: Create integration tests**

Create `Backend.Tests.Integration/Features/Antennas/AntennaControllerTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using Backend.Features.Antennas;
using Backend.Features.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using tests.Setup;

namespace tests.Features.Antennas;

public class AntennaControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly IGatewayClient _gatewayClient = Substitute.For<IGatewayClient>();

    public AntennaControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _gatewayClient.GetAntennasAsync().Returns(new List<AntennaDto>());
        _gatewayClient.GetAntennaAsync(Arg.Any<Guid>()).Returns(new AntennaDto { Id = Guid.NewGuid(), Number = 1, Status = "On" });
        _gatewayClient.UpdateAntennaAsync(Arg.Any<Guid>(), Arg.Any<UpdateAntennaDto>())
            .Returns(new AntennaDto { Id = Guid.NewGuid(), Number = 1, Status = "On" });
    }

    private HttpClient CreateClient()
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IGatewayClient>();
                services.AddSingleton(_gatewayClient);
            });
        }).CreateClient();
    }

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // --- GET /api/antennas ---

    [Fact]
    public async Task GetAntennas_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var response = await CreateClient().GetAsync("/api/antennas");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAntennas_WhenCustomerAuthenticated_ReturnsForbidden()
    {
        var client = await AuthTestHelper.CreateClientAsAsync(_factory, UserRole.Customer);
        var response = await client.GetAsync("/api/antennas");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAntennas_WhenAdminAuthenticated_ReturnsOk()
    {
        var adminClient = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IGatewayClient>();
                services.AddSingleton(_gatewayClient);
            });
        }).CreateClient();
        var token = await GetAdminTokenAsync();
        adminClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await adminClient.GetAsync("/api/antennas");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- GET /api/antennas/{id} ---

    [Fact]
    public async Task GetAntenna_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var response = await CreateClient().GetAsync($"/api/antennas/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAntenna_WhenCustomerAuthenticated_ReturnsForbidden()
    {
        var client = await AuthTestHelper.CreateClientAsAsync(_factory, UserRole.Customer);
        var response = await client.GetAsync($"/api/antennas/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // --- PUT /api/antennas/{id} ---

    [Fact]
    public async Task UpdateAntenna_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var response = await CreateClient().PutAsJsonAsync(
            $"/api/antennas/{Guid.NewGuid()}",
            new UpdateAntennaDto { Status = "On" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAntenna_WhenCustomerAuthenticated_ReturnsForbidden()
    {
        var client = await AuthTestHelper.CreateClientAsAsync(_factory, UserRole.Customer);
        var response = await client.PutAsJsonAsync(
            $"/api/antennas/{Guid.NewGuid()}",
            new UpdateAntennaDto { Status = "On" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var scopeFactory = _factory.Services.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Backend.Database.AppDbContext>();
        var user = new Backend.Features.Users.User
        {
            Name = $"Admin-{Guid.NewGuid()}",
            Email = $"admin_{Guid.NewGuid()}@test.com",
            PasswordHash = "hash",
            Role = UserRole.Admin
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return AuthTestHelper.CreateTokenForUser(user);
    }
}
```

> **Note:** The `GetAdminAuthenticated_ReturnsOk` test requires `AuthTestHelper.CreateTokenForUser` to be public. If it's currently private (named `CreateJwtToken`), make it `public static` and rename, or use `AuthTestHelper.CreateClientAsAsync` and adapt the client setup. Check `Backend.Tests.Integration/Setup/AuthTestHelper.cs` — if `CreateJwtToken` is private, add this public wrapper:
>
> ```csharp
> public static string CreateTokenForUser(User user) => CreateJwtToken(user);
> ```

- [ ] **Step 3: Run integration tests**

```bash
dotnet test Backend.Tests.Integration --filter "FullyQualifiedName~Antennas" -v minimal
```

Expected: all tests PASS (Docker must be running for Testcontainers)

- [ ] **Step 4: Run all tests to verify no regressions**

```bash
dotnet test Backend.Tests.Unit && dotnet test Backend.Tests.Integration
```

Expected: all tests PASS

- [ ] **Step 5: Commit**

```bash
git add Backend.Tests.Integration/Setup/AuthTestHelper.cs Backend.Tests.Integration/Features/Antennas/AntennaControllerTests.cs
git commit -m "test(antennas): add integration tests for auth validation"
```

---

## Task 8: Final check and push

- [ ] **Step 1: Run the full test suite**

```bash
dotnet test Backend.Tests.Unit && dotnet test Backend.Tests.Integration
```

Expected: all tests PASS

- [ ] **Step 2: Push branch**

```bash
git push -u origin feat/83-antenna-endpoints
```

- [ ] **Step 3: Link PR to issue**

When creating the pull request, include `Closes #83` in the PR description body so GitHub closes the issue automatically on merge.
