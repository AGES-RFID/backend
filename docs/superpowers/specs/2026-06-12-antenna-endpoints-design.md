# Antenna Endpoints Design

**Issue:** #83 — Criar endpoints de Antenas  
**Date:** 2026-06-12  
**Assignees:** @Bepaiva, @Fitshow

---

## Overview

Add three admin-only endpoints that allow the frontend to query and update antennas. The backend validates authentication and request data, then proxies to an external gateway. No data is stored in the database.

---

## Architecture

```
AntennaController
  └── IAntennaService
        └── IGatewayClient  ← new typed HTTP client
              └── Gateway (external HTTP service)
```

The feature follows the same Controller → Interface/Service pattern used throughout the project.

---

## Configuration

Add to `appsettings.json`:

```json
"Gateway": {
  "BaseUrl": "http://localhost:8080"
}
```

Register a typed HTTP client in `Program.cs`:

```csharp
builder.Services.AddHttpClient<IGatewayClient, GatewayClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Gateway:BaseUrl"]
        ?? throw new InvalidOperationException("Gateway:BaseUrl is required"));
});
builder.Services.AddScoped<IAntennaService, AntennaService>();
```

---

## Endpoints

All routes are under `[Route("api/antennas")]` with `[Authorize(Roles = "Admin")]` applied at the controller level.

| Method | Route               | Description              |
|--------|---------------------|--------------------------|
| GET    | `/api/antennas`     | List all antennas        |
| GET    | `/api/antennas/{id}`| Get one antenna by ID    |
| PUT    | `/api/antennas/{id}`| Update antenna settings  |

---

## DTOs

**`AntennaDto`** (response):
```json
{
  "id": "guid",
  "number": 1,
  "status": "On",
  "sensibility": 80,
  "power": 30
}
```

**`UpdateAntennaDto`** (PUT request body):
```json
{
  "status": "On",
  "sensibility": 80,
  "power": 30
}
```

---

## Validation

Applied in `AntennaService` before calling the gateway:

- `status`: if null or missing, default to `"Off"` before forwarding
- `sensibility`: if provided, must be in range `[0, 100]` → throw `ArgumentOutOfRangeException`
- `power`: if provided, must be in range `[0, 100]` → throw `ArgumentOutOfRangeException`

The controller maps `ArgumentOutOfRangeException` → `400 Bad Request`.

---

## Gateway Client

**`IGatewayClient`** interface:
```csharp
Task<List<AntennaDto>> GetAntennasAsync();
Task<AntennaDto> GetAntennaAsync(Guid id);
Task<AntennaDto> UpdateAntennaAsync(Guid id, UpdateAntennaDto dto);
```

**`GatewayClient`** implementation wraps `HttpClient`. On non-2xx responses it throws `GatewayException(int statusCode)`.

**`GatewayException`**: a custom exception that carries the upstream HTTP status code so the controller can return `502 Bad Gateway`.

---

## Error Handling

| Scenario                          | Controller response     |
|-----------------------------------|-------------------------|
| Not authenticated                 | 401 Unauthorized        |
| Authenticated as Customer         | 403 Forbidden           |
| Antenna not found (gateway 404)   | 404 Not Found           |
| Validation fails (range, etc.)    | 400 Bad Request         |
| Gateway returns non-2xx           | 502 Bad Gateway         |
| Unexpected exception              | 500 Internal Server Error |

---

## File Structure

```
Features/
  Antennas/
    Dtos/
      AntennaDto.cs
      UpdateAntennaDto.cs
    AntennaController.cs
    AntennaService.cs        (+ IAntennaService)
    GatewayClient.cs         (+ IGatewayClient)
    GatewayException.cs
```

---

## Testing

### Unit Tests (`Backend.Tests.Unit/Features/Antennas/`)

- `AntennaControllerTests`: NSubstitute mock of `IAntennaService`
  - GET all: returns 200 with list
  - GET by id: returns 200; returns 404 on `KeyNotFoundException`; returns 502 on `GatewayException`; returns 500 on unexpected exception
  - PUT: returns 200; returns 400 on `ArgumentOutOfRangeException`; returns 404 on `KeyNotFoundException`; returns 502 on `GatewayException`; returns 500 on unexpected exception

- `AntennaServiceTests`: NSubstitute mock of `IGatewayClient`
  - GET all: delegates to gateway client
  - GET by id: delegates; propagates `KeyNotFoundException` when gateway returns 404
  - PUT: defaults status to "Off" when null; validates sensibility/power range; delegates to gateway

### Integration Tests (`Backend.Tests.Integration/Features/Antennas/`)

`IGatewayClient` is mocked via `ConfigureTestServices`. Tests validate auth only:

- GET `/api/antennas`: 401 when unauthenticated, 403 when Customer, 200 when Admin
- GET `/api/antennas/{id}`: 401 when unauthenticated, 403 when Customer
- PUT `/api/antennas/{id}`: 401 when unauthenticated, 403 when Customer
