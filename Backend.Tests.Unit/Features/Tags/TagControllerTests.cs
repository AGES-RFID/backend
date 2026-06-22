using Backend.Features.Tags;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Backend.Tests.Unit.Features.Tags;

public class TagControllerTests
{
    [Fact]
    public async Task CreateTag_WhenServiceReturnsTag_ReturnsCreatedWithTag()
    {
        var dto = new CreateTagDto { Epc = "EPC-001", Tid = "TID-001" };
        var expected = new TagDto { TagId = Guid.NewGuid(), Status = "AVAILABLE", Epc = "EPC-001", Tid = "TID-001" };

        var tagService = Substitute.For<ITagService>();
        tagService.CreateTagAsync(dto).Returns(expected);

        var controller = new TagController(tagService);

        var result = await controller.CreateTag(dto);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, created.StatusCode);

        var dtoResult = Assert.IsType<TagDto>(created.Value);
        Assert.Equal(expected.TagId, dtoResult.TagId);
        Assert.Equal(expected.Status, dtoResult.Status);

        await tagService.Received(1).CreateTagAsync(Arg.Is<CreateTagDto>(x => x.Epc == dto.Epc));
    }

    [Fact]
    public async Task CreateTag_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        var tagService = Substitute.For<ITagService>();
        var controller = new TagController(tagService);
        controller.ModelState.AddModelError("Epc", "required");

        var result = await controller.CreateTag(new CreateTagDto { Epc = "EPC-001", Tid = "TID-001" });

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

        var result = await controller.CreateTag(new CreateTagDto { Epc = "EPC-001", Tid = "TID-001" });

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

        var result = await controller.CreateTag(new CreateTagDto { Epc = "EPC-001", Tid = "TID-001" });

        var error = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, error.StatusCode);
    }

    [Fact]
    public async Task CreateTagsFromCsv_WhenServiceReturnsResult_ReturnsOk()
    {
        var tagService = Substitute.For<ITagService>();
        var file = CreateCsvFile("tid,epc\nTID-001,EPC-001");
        var expected = new BulkCreateTagsResultDto { CreatedCount = 1 };
        tagService.CreateTagsFromCsvAsync(file).Returns(expected);
        var controller = new TagController(tagService);

        var result = await controller.CreateTagsFromCsv(file);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<BulkCreateTagsResultDto>(ok.Value);
        Assert.Equal(1, dto.CreatedCount);
        await tagService.Received(1).CreateTagsFromCsvAsync(file);
    }

    [Fact]
    public async Task CreateTagsFromCsv_WhenFileIsMissing_ReturnsBadRequest()
    {
        var tagService = Substitute.For<ITagService>();
        var controller = new TagController(tagService);

        var result = await controller.CreateTagsFromCsv(null!);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        await tagService.DidNotReceiveWithAnyArgs().CreateTagsFromCsvAsync(default!);
    }

    [Fact]
    public async Task GetAllTags_WhenServiceReturnsTags_ReturnsOk()
    {
        var tagId = Guid.NewGuid();
        var expected = new List<TagListDto>
        {
            new()
            {
                TagId = tagId,
                Tid = "TID-001",
                Epc = "EPC-001",
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

        Assert.Equal(tagId, tag.TagId);
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
        var tagId = Guid.NewGuid();
        var expected = new List<TagListDto>
        {
            new()
            {
                TagId = tagId,
                Tid = "TID-001",
                Epc = "EPC-001",
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
        var tagId = Guid.NewGuid();
        var expected = new TagDto { TagId = tagId, Status = "INACTIVE", Epc = "EPC-001", Tid = "TID-001" };

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
        var tagId = Guid.NewGuid();
        var tagService = Substitute.For<ITagService>();
        tagService.DeactivateTagAsync(tagId)
            .Returns(Task.FromException<TagDto>(new KeyNotFoundException("not found")));

        var controller = new TagController(tagService);

        var result = await controller.DeactivateTag(tagId);

        Assert.IsType<NotFoundObjectResult>(result.Result);
        await tagService.Received(1).DeactivateTagAsync(tagId);
    }

    [Fact]
    public async Task DeactivateTag_WhenServiceThrowsConflict_ReturnsConflict()
    {
        var tagId = Guid.NewGuid();
        var tagService = Substitute.For<ITagService>();
        tagService.DeactivateTagAsync(tagId)
            .Returns(Task.FromException<TagDto>(new TagConflictException("inactive")));

        var controller = new TagController(tagService);

        var result = await controller.DeactivateTag(tagId);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(409, conflict.StatusCode);
    }

    [Fact]
    public async Task DeactivateTag_WhenServiceThrowsUnexpectedException_ReturnsInternalServerError()
    {
        var tagId = Guid.NewGuid();
        var tagService = Substitute.For<ITagService>();
        tagService.DeactivateTagAsync(tagId)
            .Returns(Task.FromException<TagDto>(new Exception("boom")));

        var controller = new TagController(tagService);

        var result = await controller.DeactivateTag(tagId);

        var error = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, error.StatusCode);
    }

    [Fact]
    public async Task AssignVehicle_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        var tagService = Substitute.For<ITagService>();
        var controller = new TagController(tagService);
        controller.ModelState.AddModelError("VehicleId", "required");

        var result = await controller.AssignVehicle(Guid.NewGuid(), new AssignVehicleDto { VehicleId = Guid.NewGuid() });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        await tagService.DidNotReceiveWithAnyArgs().AssignVehicleAsync(default, default!);
    }

    [Fact]
    public async Task AssignVehicle_WhenServiceReturnsTag_ReturnsOk()
    {
        var tagId = Guid.NewGuid();
        var dto = new AssignVehicleDto { VehicleId = Guid.NewGuid() };
        var expected = new TagDto { TagId = tagId, Status = "IN_USE", VehicleId = dto.VehicleId, Epc = "EPC-001", Tid = "TID-001" };

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
        var tagId = Guid.NewGuid();
        var tagService = Substitute.For<ITagService>();
        tagService.AssignVehicleAsync(tagId, Arg.Any<AssignVehicleDto>())
            .Returns(Task.FromException<TagDto>(new KeyNotFoundException("not found")));

        var controller = new TagController(tagService);

        var result = await controller.AssignVehicle(tagId, new AssignVehicleDto { VehicleId = Guid.NewGuid() });

        Assert.IsType<NotFoundObjectResult>(result.Result);
        await tagService.Received(1).AssignVehicleAsync(tagId, Arg.Any<AssignVehicleDto>());
    }

    [Fact]
    public async Task AssignVehicle_WhenServiceThrowsConflict_ReturnsConflict()
    {
        var tagId = Guid.NewGuid();
        var tagService = Substitute.For<ITagService>();
        tagService.AssignVehicleAsync(tagId, Arg.Any<AssignVehicleDto>())
            .Returns(Task.FromException<TagDto>(new TagConflictException("conflict")));

        var controller = new TagController(tagService);

        var result = await controller.AssignVehicle(tagId, new AssignVehicleDto { VehicleId = Guid.NewGuid() });

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(409, conflict.StatusCode);
    }

    [Fact]
    public async Task AssignVehicle_WhenServiceThrowsUnexpectedException_ReturnsInternalServerError()
    {
        var tagId = Guid.NewGuid();
        var tagService = Substitute.For<ITagService>();
        tagService.AssignVehicleAsync(tagId, Arg.Any<AssignVehicleDto>())
            .Returns(Task.FromException<TagDto>(new Exception("boom")));

        var controller = new TagController(tagService);

        var result = await controller.AssignVehicle(tagId, new AssignVehicleDto { VehicleId = Guid.NewGuid() });

        var error = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, error.StatusCode);
    }

    private static IFormFile CreateCsvFile(string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", "tags.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/csv"
        };
    }
}
