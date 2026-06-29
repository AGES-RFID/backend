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
    Task<BulkCreateTagsResultDto> CreateTagsFromCsvAsync(IFormFile file);
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

    public async Task<BulkCreateTagsResultDto> CreateTagsFromCsvAsync(IFormFile file)
    {
        if (file.Length == 0)
            throw new ArgumentException("CSV file is empty");

        var result = new BulkCreateTagsResultDto();
        using var reader = new StreamReader(file.OpenReadStream());
        var lineNumber = 0;
        var hasHeader = false;

        string? line;
        while ((line = await reader.ReadLineAsync()) is not null)
        {
            lineNumber++;

            if (await ProcessCsvLineAsync(line, lineNumber, result))
                continue;

            var columns = ParseCsvLine(line);
            if (lineNumber == 1 && columns.Count >= 2 && IsHeader(columns))
                hasHeader = true;
        }

        if (!hasHeader && result.CreatedTags.Count == 0 && result.Errors.Count == 0)
            result.Errors.Add("CSV file does not contain tags");

        result.CreatedCount = result.CreatedTags.Count;
        return result;
    }

    private async Task<bool> ProcessCsvLineAsync(string line, int lineNumber, BulkCreateTagsResultDto result)
    {
        if (string.IsNullOrWhiteSpace(line))
            return true;

        var columns = ParseCsvLine(line);
        if (columns.Count < 2)
        {
            result.Errors.Add($"Line {lineNumber}: expected TID and EPC columns");
            return true;
        }

        if (lineNumber == 1 && IsHeader(columns))
            return false;

        var tid = columns[0].Trim();
        var epc = columns[1].Trim();

        if (string.IsNullOrWhiteSpace(tid) || string.IsNullOrWhiteSpace(epc))
        {
            result.Errors.Add($"Line {lineNumber}: TID and EPC are required");
            return true;
        }

        try
        {
            var created = await CreateTagAsync(new CreateTagDto { Tid = tid, Epc = epc });
            result.CreatedTags.Add(created);
        }
        catch (TagConflictException ex)
        {
            result.Errors.Add($"Line {lineNumber}: {ex.Message}");
        }

        return true;
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

    private static bool IsHeader(IReadOnlyList<string> columns) =>
        string.Equals(columns[0].Trim(), "tid", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(columns[1].Trim(), "epc", StringComparison.OrdinalIgnoreCase);

    private static List<string> ParseCsvLine(string line)
    {
        var columns = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                columns.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        columns.Add(current.ToString());
        return columns;
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
