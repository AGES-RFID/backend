using Backend.Features.Common.Mapping;

namespace Backend.Features.RfidTags;

public interface IRfidTagService
{
    Task<RfidTagDto> CreateTagAsync(CreateRfidTagDto dto);
    Task<RfidTagDto> DeactivateTagAsync(int id);
    Task<RfidTagDto> ReactivateTagAsync(int id);
    Task<RfidTagDto?> GetTagAsync(int id);
    Task<IEnumerable<RfidTagDto>> GetAllTagsAsync();
}

public class RfidTagService : IRfidTagService
{
    private readonly List<Backend.Features.RfidTags.Models.RfidTag> _tags = new();

    public async Task<RfidTagDto> CreateTagAsync(CreateRfidTagDto dto)
    {
        if (_tags.Any(t => t.TagNumber == dto.TagNumber))
            throw new InvalidOperationException($"Número da tag {dto.TagNumber} já existe");

        var tag = dto.ToEntity();
        tag.RfidTagId = _tags.Count + 1;

        _tags.Add(tag);

        return tag.ToDto();
    }

    public async Task<RfidTagDto> DeactivateTagAsync(int id)
    {
        var tag = _tags.FirstOrDefault(t => t.RfidTagId == id);
        if (tag == null)
            throw new KeyNotFoundException($"Tag RFID com ID {id} não encontrada");

        if (!tag.IsActive)
            throw new InvalidOperationException($"Tag RFID com ID {id} já está desativada");

        tag.IsActive = false;
        tag.DeactivatedAt = DateTime.UtcNow;
        tag.UpdatedAt = DateTime.UtcNow;

        return tag.ToDto();
    }

    public async Task<RfidTagDto> ReactivateTagAsync(int id)
    {
        var tag = _tags.FirstOrDefault(t => t.RfidTagId == id);
        if (tag == null)
            throw new KeyNotFoundException($"Tag RFID com ID {id} não encontrada");

        if (tag.IsActive)
            throw new InvalidOperationException($"Tag RFID com ID {id} já está ativa");

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
