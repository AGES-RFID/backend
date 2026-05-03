using Backend.Features.Accesses;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Backend.Tests.Unit.Features.Accesses;

public class AccessesControllerTests
{
    [Fact]
    public async Task RegisterEntry_WhenSuccess_ReturnsCreated()
    {

        var dto = new CreateAccessDto { TagId = "TAG-001" };
        var expected = new AccessDto { AccessId = Guid.NewGuid(), TagId = "TAG-001", Type = AccessType.Entry, Timestamp = DateTime.UtcNow };

        var service = Substitute.For<IAccessesService>();
        service.RegisterEntryAsync(dto).Returns(expected);

        var controller = new AccessesController(service);


        var result = await controller.RegisterEntry(dto);


        var createdResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal(expected, createdResult.Value);
    }

    [Fact]
    public async Task RegisterEntry_WhenTagNotFound_ReturnsNotFound()
    {

        var dto = new CreateAccessDto { TagId = "TAG-404" };
        var service = Substitute.For<IAccessesService>();
        service.RegisterEntryAsync(dto).ThrowsAsync(new KeyNotFoundException("Tag not found"));

        var controller = new AccessesController(service);


        var result = await controller.RegisterEntry(dto);


        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task RegisterEntry_WhenTagInactive_ReturnsConflict()
    {

        var dto = new CreateAccessDto { TagId = "TAG-003" };
        var service = Substitute.For<IAccessesService>();
        service.RegisterEntryAsync(dto).ThrowsAsync(new InvalidOperationException("Tag inactive"));

        var controller = new AccessesController(service);


        var result = await controller.RegisterEntry(dto);


        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task RegisterExit_WhenAlreadyOutside_ReturnsConflict()
    {

        var dto = new CreateAccessDto { TagId = "TAG-002" };
        var service = Substitute.For<IAccessesService>();
        service.RegisterExitAsync(dto).ThrowsAsync(new InvalidOperationException("Already outside"));

        var controller = new AccessesController(service);


        var result = await controller.RegisterExit(dto);


        Assert.IsType<ConflictObjectResult>(result.Result);
    }
}
