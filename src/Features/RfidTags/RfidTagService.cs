using Backend.Features.Common.Mapping;
using Backend.Features.Common.Services;

namespace Backend.Features.RfidTags;

public interface IRfidTagService
{
    Task<RfidTagDto> CreateTagAsync(CreateRfidTagDto dto);
    Task<RfidTagDto> DeactivateTagAsync(int id);
    Task<RfidTagDto> ReactivateTagAsync(int id);
    Task<RfidTagDto?> GetTagAsync(int id);
    Task<IEnumerable<RfidTagDto>> GetAllTagsAsync();
}

public class RfidTagService : BaseService, IRfidTagService
{
    private readonly List<Backend.Features.RfidTags.Models.RfidTag> _tags = new();

    public async Task<RfidTagDto> CreateTagAsync(CreateRfidTagDto dto)
    {
        var existingTag = _tags.FirstOrDefault(t => t.TagNumber == dto.TagNumber);
        ValidateExists(existingTag, "Número da tag", dto.TagNumber);

        var tag = dto.ToEntity();
        tag.RfidTagId = GenerateId(_tags);

        _tags.Add(tag);

        return tag.ToDto();
    }

    public async Task<RfidTagDto> DeactivateTagAsync(int id)
    {
        var tag = _tags.FirstOrDefault(t => t.RfidTagId == id);
        ValidateNotNull(tag, "Tag RFID", id);

        ValidateActive(tag.IsActive, "Tag RFID", id);

        tag.IsActive = false;
        tag.DeactivatedAt = DateTime.UtcNow;
        tag.UpdatedAt = DateTime.UtcNow;

        return tag.ToDto();
    }

    public async Task<RfidTagDto> ReactivateTagAsync(int id)
    {
        var tag = _tags.FirstOrDefault(t => t.RfidTagId == id);
        ValidateNotNull(tag, "Tag RFID", id);

        ValidateInactive(tag.IsActive, "Tag RFID", id);

        tag.IsActive = true;
        tag.ReactivatedAt = DateTime.UtcNow;
        tag.UpdatedAt = DateTime.UtcNow;

        return tag.ToDto();
    }

    public async Task<RfidTagDto?> GetTagAsync(int id)
    {
        var tag = _tags.FirstOrDefault(t => t.RfidTagId == id);
        if (tag == null)
            return null;

        return tag.ToDto();
    }

    public async Task<IEnumerable<RfidTagDto>> GetAllTagsAsync()
    {
        return _tags.ToDtos();
    }
}
