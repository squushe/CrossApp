namespace Core.Dto;

public record ProductDto(
    string Id,
    string Name,
    decimal Price
) : DomainRecord;
