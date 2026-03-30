using Backend.Features.RfidTags.Dtos;
using Backend.Features.RfidTags.Models;

namespace Backend.Features.Common.Mapping;

public static class RfidTagMapper
{
    public static RfidTagDto ToDto(this RfidTag rfidTag)
    {
        return new RfidTagDto
        {
            Id = rfidTag.RfidTagId,
            TagNumber = rfidTag.TagNumber,
            IsActive = rfidTag.IsActive,
            CreatedAt = rfidTag.CreatedAt,
            DeactivatedAt = rfidTag.DeactivatedAt,
            ReactivatedAt = rfidTag.ReactivatedAt,
            UpdatedAt = rfidTag.UpdatedAt
        };
    }

    public static RfidTag ToEntity(this CreateRfidTagDto dto)
    {
        return new RfidTag
        {
            TagNumber = dto.TagNumber,
            IsActive = true
        };
    }

    public static IEnumerable<RfidTagDto> ToDtos(this IEnumerable<RfidTag> rfidTags)
    {
        return rfidTags.Select(rfidTag => rfidTag.ToDto());
    }
}
