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

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<AccessFailureResponseDto>(notFound.Value);
        Assert.False(response.Success);
        Assert.Equal("tag_not_found", response.Reason);
        Assert.Equal("Tag not found", response.Message);
    }

    [Fact]
    public async Task RegisterAccess_WhenTagInactive_ReturnsConflict()
    {
        var dto = MakeDto();
        var service = Substitute.For<IAccessesService>();
        service.RegisterAccessAsync(dto).ThrowsAsync(new InvalidOperationException("Tag inactive"));

        var controller = new AccessesController(service);

        var result = await controller.RegisterAccess(dto);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        var response = Assert.IsType<AccessFailureResponseDto>(conflict.Value);
        Assert.False(response.Success);
        Assert.Equal("access_registration_failed", response.Reason);
        Assert.Equal("Tag inactive", response.Message);
    }

    [Fact]
    public async Task RegisterAccess_Exit_WhenAlreadyOutside_ReturnsConflict()
    {
        var dto = MakeDto(entrance: false);
        var service = Substitute.For<IAccessesService>();
        service.RegisterAccessAsync(dto).ThrowsAsync(new AccessRegistrationConflictException(
            "tag_already_outside",
            "Access registration failed because this tag is already outside the parking lot.",
            "The tag is already outside the parking lot. Exit was not registered."));

        var controller = new AccessesController(service);

        var result = await controller.RegisterAccess(dto);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        var response = Assert.IsType<AccessFailureResponseDto>(conflict.Value);
        Assert.False(response.Success);
        Assert.Equal("tag_already_outside", response.Reason);
        Assert.Equal("The tag is already outside the parking lot. Exit was not registered.", response.Warning);
    }

    [Fact]
    public async Task RegisterAccess_Entry_WhenAlreadyInside_ReturnsConflictWithWarning()
    {
        var dto = MakeDto(entrance: true);
        var service = Substitute.For<IAccessesService>();
        service.RegisterAccessAsync(dto).ThrowsAsync(new AccessRegistrationConflictException(
            "tag_already_inside",
            "Access registration failed because this tag is already inside the parking lot.",
            "The tag is already inside the parking lot. Entry was not registered."));

        var controller = new AccessesController(service);

        var result = await controller.RegisterAccess(dto);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        var response = Assert.IsType<AccessFailureResponseDto>(conflict.Value);
        Assert.False(response.Success);
        Assert.Equal("tag_already_inside", response.Reason);
        Assert.Equal("The tag is already inside the parking lot. Entry was not registered.", response.Warning);
    }

    [Fact]
    public async Task GetAccesses_ReturnsOkWithAccesses()
    {
        var expected = new List<AccessDto>
        {
            new() { AccessId = Guid.NewGuid(), TagId = Guid.NewGuid(), Type = AccessType.Entry, Timestamp = DateTime.UtcNow }
        };

        var service = Substitute.For<IAccessesService>();
        service.GetAccessesAsync(null).Returns(expected);

        var controller = new AccessesController(service);

        var result = await controller.GetAccesses(null);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(expected, okResult.Value);
    }

    [Fact]
    public async Task GetAccesses_WithInvalidType_ReturnsBadRequest()
    {
        var service = Substitute.For<IAccessesService>();
        var controller = new AccessesController(service);

        var result = await controller.GetAccesses("invalid_type");

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task GetTimeseries_ReturnsOkWithData()
    {
        var expected = new TimeseriesResponseDto
        {
            From = DateTime.UtcNow.AddHours(-24),
            To = DateTime.UtcNow,
            Series = []
        };

        var service = Substitute.For<IAccessesService>();
        service.GetTimeSeriesAsync().Returns(expected);

        var controller = new AccessesController(service);

        var result = await controller.GetTimeseries();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(expected, okResult.Value);
    }
}
