using Backend.Features.Accesses;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Backend.Tests.Unit.Features.Accesses;

public class AccessesControllerTests
{
    private static CreateAccessDto MakeDto(bool entrance = true) =>
        new() { Tid = "TID-001", Epc = "EPC-001", Entrance = entrance };

    [Fact]
    public async Task RegisterAccess_Entry_WhenSuccess_ReturnsCreated()
    {
        var dto = MakeDto(entrance: true);
        var expected = new AccessDto { AccessId = Guid.NewGuid(), TagId = Guid.NewGuid(), Type = AccessType.Entry, Timestamp = DateTime.UtcNow };

        var service = Substitute.For<IAccessesService>();
        service.RegisterAccessAsync(dto).Returns(expected);

        var controller = new AccessesController(service);

        var result = await controller.RegisterAccess(dto);

        var createdResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal(expected, createdResult.Value);
    }

    [Fact]
    public async Task RegisterAccess_WhenTagNotFound_ReturnsNotFound()
    {
        var dto = MakeDto();
        var service = Substitute.For<IAccessesService>();
        service.RegisterAccessAsync(dto).ThrowsAsync(new KeyNotFoundException("Tag not found"));

        var controller = new AccessesController(service);

        var result = await controller.RegisterAccess(dto);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task RegisterAccess_WhenTagInactive_ReturnsConflict()
    {
        var dto = MakeDto();
        var service = Substitute.For<IAccessesService>();
        service.RegisterAccessAsync(dto).ThrowsAsync(new InvalidOperationException("Tag inactive"));

        var controller = new AccessesController(service);

        var result = await controller.RegisterAccess(dto);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task RegisterAccess_Exit_WhenAlreadyOutside_ReturnsConflict()
    {
        var dto = MakeDto(entrance: false);
        var service = Substitute.For<IAccessesService>();
        service.RegisterAccessAsync(dto).ThrowsAsync(new InvalidOperationException("Already outside"));

        var controller = new AccessesController(service);

        var result = await controller.RegisterAccess(dto);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }
}
