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
    Task<TagDto> DeactivateTagAsync(Guid tagId);
    Task<TagDto> AssignVehicleAsync(Guid tagId, AssignVehicleDto dto);
}

public class TagService(AppDbContext db) : ITagService
{
    private readonly AppDbContext _db = db;

    public async Task<TagDto> CreateTagAsync(CreateTagDto dto)
    {
        var existingTid = await _db.Tags.FirstOrDefaultAsync(t => t.Tid == dto.Tid);
        if (existingTid != null)
        {
            throw new TagConflictException($"A tag with TID '{dto.Tid}' already exists");
        }

        var existingEpc = await _db.Tags.FirstOrDefaultAsync(t => t.Epc == dto.Epc);
        if (existingEpc != null)
        {
            throw new TagConflictException($"A tag with Epc '{dto.Epc}' already exists");
        }

        var tag = new Tag
        {
            Epc = dto.Epc,
            Tid = dto.Tid,
            Status = TagStatus.AVAILABLE
        };

        var vehicleWithoutTag = await _db.Vehicles
            .Where(v => v.TagId == null)
            .OrderBy(v => v.CreatedAt)
            .ThenBy(v => v.VehicleId)
            .FirstOrDefaultAsync();

        if (vehicleWithoutTag is not null)
        {
            vehicleWithoutTag.TagId = tag.TagId;
            vehicleWithoutTag.Tag = tag;
            tag.Vehicle = vehicleWithoutTag;
            tag.Status = TagStatus.IN_USE;
        }

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
                throw new ArgumentException($"Invalid status value. Must be one of: {string.Join(", ", Enum.GetNames<TagStatus>())}");
            }
            filterStatus = parsedStatus;
        }

        var tags = await _db.Tags.AsNoTracking()
            .Where(t => !filterStatus.HasValue || t.Status == filterStatus.Value)
            .Select(t => new TagListDto
            {
                TagId = t.TagId,
                Epc = t.Epc,
                Tid = t.Tid,
                UserName = t.Vehicle != null ? t.Vehicle.User!.Name : null,
                Plate = t.Vehicle != null ? t.Vehicle.Plate : null,
                Status = t.Status.ToString()
            })
            .ToListAsync();

        return tags;
    }

    public async Task<TagDto> DeactivateTagAsync(Guid tagId)
    {
        var tag = await _db.Tags
            .Include(t => t.Vehicle)
            .FirstOrDefaultAsync(t => t.TagId == tagId)
            ?? throw new KeyNotFoundException($"Tag with id {tagId} not found");

        if (tag.Status == TagStatus.INACTIVE)
        {
            throw new TagConflictException($"Tag is already inactive");
        }

        if (tag.Vehicle != null)
        {
            tag.Vehicle.TagId = null;
            tag.Vehicle = null;
        }

        tag.Status = TagStatus.INACTIVE;
        tag.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return TagDto.FromModel(tag);
    }

    public async Task<TagDto> AssignVehicleAsync(Guid tagId, AssignVehicleDto dto)
    {
        var tag = await _db.Tags.FirstOrDefaultAsync(t => t.TagId == tagId)
            ?? throw new KeyNotFoundException($"Tag with id {tagId} not found");

        // Check if vehicle exists
        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == dto.VehicleId)
            ?? throw new KeyNotFoundException($"Vehicle with id {dto.VehicleId} not found");

        // Check if tag is already assigned
        if (tag.Status == TagStatus.IN_USE)
        {
            throw new TagConflictException($"Tag is already assigned to a vehicle");
        }

        // Check if tag is inactive
        if (tag.Status == TagStatus.INACTIVE)
        {
            throw new TagConflictException($"Cannot assign an inactive tag to a vehicle");
        }

        // Check if vehicle already has a tag assigned
        if (vehicle.TagId != null)
        {
            throw new TagConflictException($"Vehicle already has a tag assigned");
        }

        vehicle.TagId = tag.TagId;
        tag.Vehicle = vehicle;
        tag.Status = TagStatus.IN_USE;
        tag.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return TagDto.FromModel(tag);
    }
}
