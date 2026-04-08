using Backend.Database;
using Backend.Features.Tags.Enums;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Tags;

public class TagConflictException : Exception
{
    public TagConflictException(string message) : base(message) { }
}

public interface ITagService
{
    Task<TagDto> CreateTagAsync(CreateTagDto dto);
    Task<IEnumerable<TagListDto>> GetAllTagsAsync(string? status);
    Task<TagDto> DeactivateTagAsync(string tagId);
    Task<TagDto> AssignVehicleAsync(string tagId, AssignVehicleDto dto);
}

public class TagService(AppDbContext db) : ITagService
{
    private readonly AppDbContext _db = db;

    public async Task<TagDto> CreateTagAsync(CreateTagDto dto)
    {
        // Check if tag already exists
        var existingTag = await _db.Tags.FirstOrDefaultAsync(t => t.TagId == dto.TagId);
        if (existingTag != null)
        {
            throw new TagConflictException($"A tag with id '{dto.TagId}' already exists");
        }

        var tag = new Tag
        {
            TagId = dto.TagId,
            Status = TagStatus.AVAILABLE
        };

        await _db.Tags.AddAsync(tag);
        await _db.SaveChangesAsync();

        return TagDto.FromModel(tag);
    }

    public async Task<IEnumerable<TagListDto>> GetAllTagsAsync(string? status)
    {
        // Parse and validate status filter if provided
        TagStatus? filterStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<TagStatus>(status, ignoreCase: true, out var parsedStatus))
            {
                throw new ArgumentException($"Invalid status value. Must be one of: {string.Join(", ", Enum.GetNames(typeof(TagStatus)))}");
            }
            filterStatus = parsedStatus;
        }

        var tags = await _db.Tags.AsNoTracking()
            .Where(t => !filterStatus.HasValue || t.Status == filterStatus.Value)
            .Select(t => new TagListDto
            {
                Id = t.TagId,
                UserName = t.Vehicle != null ? t.Vehicle.User.Name : null,
                Plate = t.Vehicle != null ? t.Vehicle.LicensePlate : null,
                Status = t.Status.ToString()
            })
            .ToListAsync();

        return tags;
    }

    public async Task<TagDto> DeactivateTagAsync(string tagId)
    {
        var tag = await _db.Tags.FirstOrDefaultAsync(t => t.TagId == tagId)
            ?? throw new KeyNotFoundException($"Tag with id {tagId} not found");

        if (tag.Status == TagStatus.INACTIVE)
        {
            throw new TagConflictException($"Tag is already inactive");
        }

        tag.Status = TagStatus.INACTIVE;
        tag.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return TagDto.FromModel(tag);
    }

    public async Task<TagDto> AssignVehicleAsync(string tagId, AssignVehicleDto dto)
    {
        var tag = await _db.Tags.FirstOrDefaultAsync(t => t.TagId == tagId)
            ?? throw new KeyNotFoundException($"Tag with id {tagId} not found");

        // Check if vehicle exists
        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == dto.VehicleId)
            ?? throw new KeyNotFoundException($"Vehicle with id {dto.VehicleId} not found");  

        // Check if tag is already assigned
        if (tag.Status == TagStatus.IN_USE  && tag.Vehicle != null)
        {
            throw new TagConflictException($"Tag is already assigned to a vehicle");
        }

        // Check if tag is inactive
        if (tag.Status == TagStatus.INACTIVE)
        {
            throw new TagConflictException($"Cannot assign an inactive tag to a vehicle");
        }

        // Check if vehicle already has an active tag
        if (vehicle.TagId != null)
        {
            throw new TagConflictException($"Vehicle already has a tag assigned");
        }

        tag.VehicleId = dto.VehicleId;
        tag.Status = TagStatus.IN_USE;
        tag.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return TagDto.FromModel(tag);
    }
}
