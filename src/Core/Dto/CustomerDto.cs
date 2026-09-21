namespace Core.Dto;

public record CustomerDto(
    string Id,
    string Name,
    string Email,
    string? Phone = null
) : DomainRecord;
