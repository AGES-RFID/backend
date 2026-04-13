using Backend.Features.Tags;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Backend.Tests.Unit.Features.Tags;

public class TagControllerTests
{
    [Fact]
    public async Task CreateTag_WhenServiceReturnsTag_ReturnsCreatedWithTag()
    {
        var dto = new CreateTagDto { TagId = "TAG-001" };
        var expected = new TagDto { TagId = dto.TagId, Status = "AVAILABLE" };

        var tagService = Substitute.For<ITagService>();
        tagService.CreateTagAsync(dto).Returns(expected);

        var controller = new TagController(tagService);

        var result = await controller.CreateTag(dto);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, created.StatusCode);

        var dtoResult = Assert.IsType<TagDto>(created.Value);
        Assert.Equal(expected.TagId, dtoResult.TagId);
        Assert.Equal(expected.Status, dtoResult.Status);

        await tagService.Received(1).CreateTagAsync(Arg.Is<CreateTagDto>(x => x.TagId == dto.TagId));
    }

    [Fact]
    public async Task CreateTag_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        var tagService = Substitute.For<ITagService>();
        var controller = new TagController(tagService);
        controller.ModelState.AddModelError("TagId", "required");

        var result = await controller.CreateTag(new CreateTagDto { TagId = "TAG-001" });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        await tagService.DidNotReceiveWithAnyArgs().CreateTagAsync(default!);
    }

    [Fact]
    public async Task CreateTag_WhenServiceThrowsConflict_ReturnsConflict()
    {
        var tagService = Substitute.For<ITagService>();
        tagService.CreateTagAsync(Arg.Any<CreateTagDto>())
            .Returns(Task.FromException<TagDto>(new TagConflictException("already exists")));

        var controller = new TagController(tagService);

        var result = await controller.CreateTag(new CreateTagDto { TagId = "TAG-001" });

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(409, conflict.StatusCode);
    }

    [Fact]
    public async Task CreateTag_WhenServiceThrowsUnexpectedException_ReturnsInternalServerError()
    {
        var tagService = Substitute.For<ITagService>();
        tagService.CreateTagAsync(Arg.Any<CreateTagDto>())
            .Returns(Task.FromException<TagDto>(new Exception("boom")));

        var controller = new TagController(tagService);

        var result = await controller.CreateTag(new CreateTagDto { TagId = "TAG-001" });

        var error = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, error.StatusCode);
    }

    [Fact]
    public async Task GetAllTags_WhenServiceReturnsTags_ReturnsOk()
    {
        var expected = new List<TagListDto>
        {
            new()
            {
                Id = "TAG-001",
                UserName = "Alice",
                Plate = "ABC1D23",
                Status = "AVAILABLE"
            }
        };

        var tagService = Substitute.For<ITagService>();
        tagService.GetAllTagsAsync("AVAILABLE").Returns(expected);

        var controller = new TagController(tagService);

        var result = await controller.GetAllTags("AVAILABLE");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsAssignableFrom<IEnumerable<TagListDto>>(ok.Value);
        var tag = Assert.Single(dto);

        Assert.Equal("TAG-001", tag.Id);
        Assert.Equal("Alice", tag.UserName);
        Assert.Equal("ABC1D23", tag.Plate);
        Assert.Equal("AVAILABLE", tag.Status);

        await tagService.Received(1).GetAllTagsAsync("AVAILABLE");
    }

    [Fact]
    public async Task GetAllTags_WhenServiceThrowsArgumentException_ReturnsBadRequest()
    {
        var tagService = Substitute.For<ITagService>();
        tagService.GetAllTagsAsync("INVALID")
            .Returns(Task.FromException<IEnumerable<TagListDto>>(new ArgumentException("invalid status")));

        var controller = new TagController(tagService);

        var result = await controller.GetAllTags("INVALID");

        Assert.IsType<BadRequestObjectResult>(result.Result);
        await tagService.Received(1).GetAllTagsAsync("INVALID");
    }

    [Fact]
    public async Task GetAllTags_WhenServiceThrowsUnexpectedException_ReturnsInternalServerError()
    {
        var tagService = Substitute.For<ITagService>();
        tagService.GetAllTagsAsync("AVAILABLE")
            .Returns(Task.FromException<IEnumerable<TagListDto>>(new Exception("boom")));

        var controller = new TagController(tagService);

        var result = await controller.GetAllTags("AVAILABLE");

        var error = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, error.StatusCode);
    }

    [Fact]
    public async Task GetAllTags_WhenStatusIsProvided_PassesStatusToService()
    {
        var expected = new List<TagListDto>
        {
            new()
            {
                Id = "TAG-001",
                UserName = "Alice",
                Plate = "ABC1D23",
                Status = "AVAILABLE"
            }
        };

        var tagService = Substitute.For<ITagService>();
        tagService.GetAllTagsAsync("AVAILABLE").Returns(expected);

        var controller = new TagController(tagService);

        var result = await controller.GetAllTags("AVAILABLE");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsAssignableFrom<IEnumerable<TagListDto>>(ok.Value);
        var tag = Assert.Single(dto);

        Assert.Equal("AVAILABLE", tag.Status);
        await tagService.Received(1).GetAllTagsAsync("AVAILABLE");
    }

    [Fact]
    public async Task DeactivateTag_WhenServiceReturnsTag_ReturnsOk()
    {
        var tagId = "TAG-001";
        var expected = new TagDto { TagId = tagId, Status = "INACTIVE" };

        var tagService = Substitute.For<ITagService>();
        tagService.DeactivateTagAsync(tagId).Returns(expected);

        var controller = new TagController(tagService);

        var result = await controller.DeactivateTag(tagId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<TagDto>(ok.Value);

        Assert.Equal(tagId, dto.TagId);
        Assert.Equal("INACTIVE", dto.Status);

        await tagService.Received(1).DeactivateTagAsync(tagId);
    }

    [Fact]
    public async Task DeactivateTag_WhenServiceThrowsNotFound_ReturnsNotFound()
    {
        var tagService = Substitute.For<ITagService>();
        tagService.DeactivateTagAsync("TAG-404")
            .Returns(Task.FromException<TagDto>(new KeyNotFoundException("not found")));

        var controller = new TagController(tagService);

        var result = await controller.DeactivateTag("TAG-404");

        Assert.IsType<NotFoundObjectResult>(result.Result);
        await tagService.Received(1).DeactivateTagAsync("TAG-404");
    }

    [Fact]
    public async Task DeactivateTag_WhenServiceThrowsConflict_ReturnsConflict()
    {
        var tagService = Substitute.For<ITagService>();
        tagService.DeactivateTagAsync("TAG-001")
            .Returns(Task.FromException<TagDto>(new TagConflictException("inactive")));

        var controller = new TagController(tagService);

        var result = await controller.DeactivateTag("TAG-001");

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(409, conflict.StatusCode);
    }

    [Fact]
    public async Task DeactivateTag_WhenServiceThrowsUnexpectedException_ReturnsInternalServerError()
    {
        var tagService = Substitute.For<ITagService>();
        tagService.DeactivateTagAsync("TAG-001")
            .Returns(Task.FromException<TagDto>(new Exception("boom")));

        var controller = new TagController(tagService);

        var result = await controller.DeactivateTag("TAG-001");

        var error = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, error.StatusCode);
    }

    [Fact]
    public async Task AssignVehicle_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        var tagService = Substitute.For<ITagService>();
        var controller = new TagController(tagService);
        controller.ModelState.AddModelError("VehicleId", "required");

        var result = await controller.AssignVehicle("TAG-001", new AssignVehicleDto { VehicleId = Guid.NewGuid() });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        await tagService.DidNotReceiveWithAnyArgs().AssignVehicleAsync(default!, default!);
    }

    [Fact]
    public async Task AssignVehicle_WhenServiceReturnsTag_ReturnsOk()
    {
        var tagId = "TAG-001";
        var dto = new AssignVehicleDto { VehicleId = Guid.NewGuid() };
        var expected = new TagDto { TagId = tagId, Status = "IN_USE", VehicleId = dto.VehicleId };

        var tagService = Substitute.For<ITagService>();
        tagService.AssignVehicleAsync(tagId, dto).Returns(expected);

        var controller = new TagController(tagService);

        var result = await controller.AssignVehicle(tagId, dto);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<TagDto>(ok.Value);

        Assert.Equal(tagId, response.TagId);
        Assert.Equal("IN_USE", response.Status);
        Assert.Equal(dto.VehicleId, response.VehicleId);

        await tagService.Received(1).AssignVehicleAsync(tagId, Arg.Is<AssignVehicleDto>(x => x.VehicleId == dto.VehicleId));
    }

    [Fact]
    public async Task AssignVehicle_WhenServiceThrowsNotFound_ReturnsNotFound()
    {
        var tagService = Substitute.For<ITagService>();
        tagService.AssignVehicleAsync("TAG-404", Arg.Any<AssignVehicleDto>())
            .Returns(Task.FromException<TagDto>(new KeyNotFoundException("not found")));

        var controller = new TagController(tagService);

        var result = await controller.AssignVehicle("TAG-404", new AssignVehicleDto { VehicleId = Guid.NewGuid() });

        Assert.IsType<NotFoundObjectResult>(result.Result);
        await tagService.Received(1).AssignVehicleAsync("TAG-404", Arg.Any<AssignVehicleDto>());
    }

    [Fact]
    public async Task AssignVehicle_WhenServiceThrowsConflict_ReturnsConflict()
    {
        var tagService = Substitute.For<ITagService>();
        tagService.AssignVehicleAsync("TAG-001", Arg.Any<AssignVehicleDto>())
            .Returns(Task.FromException<TagDto>(new TagConflictException("conflict")));

        var controller = new TagController(tagService);

        var result = await controller.AssignVehicle("TAG-001", new AssignVehicleDto { VehicleId = Guid.NewGuid() });

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(409, conflict.StatusCode);
    }

    [Fact]
    public async Task AssignVehicle_WhenServiceThrowsUnexpectedException_ReturnsInternalServerError()
    {
        var tagService = Substitute.For<ITagService>();
        tagService.AssignVehicleAsync("TAG-001", Arg.Any<AssignVehicleDto>())
            .Returns(Task.FromException<TagDto>(new Exception("boom")));

        var controller = new TagController(tagService);

        var result = await controller.AssignVehicle("TAG-001", new AssignVehicleDto { VehicleId = Guid.NewGuid() });

        var error = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, error.StatusCode);
    }
}